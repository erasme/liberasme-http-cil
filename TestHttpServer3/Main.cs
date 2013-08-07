using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Erasme.Http;

namespace TestHttpServer3
{
	public class BufferManager<T> where T: class
	{
		object instanceLock = new object();
		LinkedList<T> available = new LinkedList<T>();

		public BufferManager()
		{
		}

		public LinkedListNode<T> Get()
		{
			LinkedListNode<T> node;
			lock(instanceLock) {
				node = available.First;
				if(node != null)
					available.RemoveFirst();
			}
			if(node == null)
				return null;
			else
				return node;
		}

		public void Release(LinkedListNode<T> e)
		{
			lock(instanceLock) {
				available.AddFirst(e);
			}
		}
	}



	public class Server
	{
		Socket listener;
//		static byte[] content = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\ncontent-type: text/plain\r\ncontent-length: 14\r\n\r\nHello World!\r\n");
//		static byte[] buffer = new byte[4096];

//		BufferManager<SocketAsyncEventArgs> buffers = new BufferManager<SocketAsyncEventArgs>();
		BufferManager<byte[]> buffers = new BufferManager<byte[]>();
		BufferManager<NetStream> netStreams = new BufferManager<NetStream>();

//		BufferManager<HttpServerClient> httpClients = new BufferManager<HttpServerClient>();

		public Server(int port)
		{
			listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
			listener.Bind(endPoint);
			listener.Listen(1024);


		}

		public void Start()
		{
			SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
			acceptEventArgs.Completed += AcceptCallback;
			StartAccept(acceptEventArgs);
		}

		void StartAccept(SocketAsyncEventArgs acceptEventArgs)
		{
			acceptEventArgs.AcceptSocket = null;
			if(!listener.AcceptAsync(acceptEventArgs))
				AcceptCallback(listener, acceptEventArgs);
		}

		public static bool ReadLine(byte[] buffer, int offset, int size, StringBuilder sb, ref bool cr, out int used)
		{
			used = 0;
			for(int i = offset; i < size+offset; i++) {
				used++;
				if(cr) {
					// LF
					if(buffer[i] == 10) {
						cr = false;
						return true;
					}
					else {
						throw new Exception("Invalid line format");
					}
				}
				else {
					// CR
					if(buffer[i] == 13) {
						cr = true;
					}
					else if(buffer[i] == 10) {
						throw new Exception("Invalid line format");
					}
					else {
						sb.Append((char)buffer[i]);
					}
				}
			}
			return false;
		}

		public static async Task<ArraySegment<byte>> ReadHeaders(Stream stream, StringBuilder sb, ArraySegment<byte> segment, Dictionary<string,string> headers)
		{
			bool cr = false;
			int used = 0;
			string line = null;
			int offset = segment.Offset;
			int size = segment.Count;
			byte[] buffer = segment.Array;

			while(true) {
				// read a line
				if(ReadLine(buffer, offset, size, sb, ref cr, out used)) {
					string line2 = sb.ToString();
					sb.Clear();
					// multi lines header
					if((line != null) && (line2.StartsWith(" ") || line2.StartsWith("\t"))) {
						line += line2;
					}
					// new header
					else {
						// flush previous line
						if(line != null) {
							int pos = line.IndexOf(':');
							if((pos == -1) || (pos == 0))
								throw new Exception("Invalid mime header line. Missing ':'");
							string key = line.Substring(0, pos).ToLower();
							string content = line.Substring(pos+2, line.Length - (pos+2));
							headers[key] = HttpUtility.QuotedPrintableDecode(content);
						}
						line = line2;
						if(line == String.Empty)
							return new ArraySegment<byte>(buffer, offset+used, size-used);
					}
				}
				offset += used;
				size -= used;
				if(size <= 0) {
					offset = 0;
					size = await stream.ReadAsync(buffer, 0, buffer.Length);
				}
			}
		}

/*		async Task<HttpServerRequest> ReadRequestAsync(StringBuilder sb, BufferContext bufferContext)
		{
			bool cr = false;
			sb.Clear();

			if(bufferContext.Count == 0)
				await bufferContext.Fill();
			// read the command line
			while(!HttpUtility.ReadLine(bufferContext, sb, ref cr)) {
				await bufferContext.Fill();
			}
			string command = sb.ToString();
			DateTime startTime = DateTime.Now;
			sb.Clear();
			// read the headers
			HttpHeaders headers = new HttpHeaders();
			await HttpUtility.ReadHeaders(bufferContext, sb, headers);

			return new HttpServerRequest(null, command, headers, startTime, 0);
		}*/


		async void AcceptCallback(object sender, SocketAsyncEventArgs acceptEventArgs)
		{
			if(acceptEventArgs.SocketError == SocketError.Success) {
				Socket socket = acceptEventArgs.AcceptSocket;
				StartAccept(acceptEventArgs);

				byte[] buffer;
				LinkedListNode<byte[]> bufferNode = buffers.Get();
//				Console.WriteLine("Buffers.Get: "+buffer);
				if(bufferNode == null) {
					buffer = new byte[4096];
					bufferNode = new LinkedListNode<byte[]>(buffer);
				}
				else {
					buffer = bufferNode.Value;
				}

//				MainClass.TestFactory.StartNew(async () => {

//				Console.WriteLine("Step1: "+Thread.CurrentThread.ManagedThreadId);

//				NetworkStream stream = new NetworkStream(socket, true);
				NetStream stream;
				LinkedListNode<NetStream> streamNode = netStreams.Get();
				if(streamNode != null) {
					stream = streamNode.Value;
					stream.SetSocket(socket);
				}
				else {
					stream = new NetStream(socket, true);
					streamNode = new LinkedListNode<NetStream>(stream);
				}

//				HttpServerClient client;
//				LinkedListNode<HttpServerClient> clientNode = httpClients.Get();
//				if(clientNode != null) {
//					client = clientNode.Value;
//					client.Reset(socket);
//				}
//				else {
//					client = new HttpServerClient(null, socket);
//					clientNode = new LinkedListNode<HttpServerClient>(client);
//				}

//				await client.ProcessAsync();

//				client.Close();

//				NetStream stream = new NetStream(socket, true);
//				stream.Read(buffer, 0, buffer.Length);
//				stream.Write(content, 0, content.Length);

//				using(Stream bufferedStream = new ReadBufferedStream(stream, 4096)) {

				BufferContext bufferContext = new BufferContext();
				bufferContext.Stream = stream;
				bufferContext.Offset = 0;
				bufferContext.Count = 0;
				bufferContext.Buffer = buffer;

//				string command = HttpUtility.ReadLine(buffer,  bufferedStream);
//				Dictionary<string,string> headers = HttpUtility.ReadHeaders(buffer, bufferedStream);

//				int size = await bufferedStream.ReadAsync(buffer, 0, buffer.Length);

				StringBuilder sb = new StringBuilder(128);
//				int used;
//				int offset = 0;
//				bool cr = false;
//				await bufferContext.Fill();
//				while(!HttpUtility.ReadLine(bufferContext, sb, ref cr)) {
//					await bufferContext.Fill();
//				}
//				string command = sb.ToString();
				string command = await HttpUtility.ReadLineAsync(bufferContext, sb);
//				Console.WriteLine(sb.ToString());

//				ReadLine(buffer, offset, size, sb, ref cr, out used);
//				offset += used;
//				size -= used;
//				Console.WriteLine("First line: "+sb.ToString()+", used: "+used+", offset: "+offset+", size: "+size+", res: "+res);

				sb.Clear();

//				res = ReadLine(buffer, offset, size, sb, ref cr, out used);
//				offset += used;
//				size -= used;
//				Console.WriteLine("Second line: "+sb.ToString()+", used: "+used+", offset: "+offset+", size: "+size+", res: "+res);

				HttpHeaders headers = new HttpHeaders();
				if((command != null) && await HttpUtility.ReadHeadersAsync(bufferContext, sb, headers)) {

					HttpRequest request = new HttpRequest(bufferContext, command, headers, DateTime.Now);

					// finish reading request input stream if needed
					while(await request.InputStream.ReadAsync(null, 0, int.MaxValue) > 0) {}

//					HttpServerRequest request = await ReadRequestAsync(new StringBuilder(128), bufferContext);

//					foreach(string key in headers.Keys) {
//						Console.WriteLine(key+" => "+headers[key]);
//					}

					HttpServerResponse response = new HttpServerResponse();
					response.StatusCode = 200;
					response.Content = new StringContent("Hello World !\r\n");
					long length;
					response.Content.TryComputeLength(out length);
					response.Headers["content-length"] = length.ToString();

					// compute the headers into memory
					Stream memStream = new MemoryStream();
					byte[] stringBuffer = Encoding.UTF8.GetBytes("HTTP/1.1 "+response.Status+"\r\n");
					memStream.Write(stringBuffer, 0, stringBuffer.Length);
					HttpUtility.HeadersToStream(response.Headers, memStream);

//					byte[] stringBuffer = Encoding.UTF8.GetBytes("HTTP/1.1 "+response.Status+"\r\n");
//					stream.Write(stringBuffer, 0, stringBuffer.Length);
//					HttpUtility.HeadersToStream(response.Headers, stream);

					// send the headers
					memStream.Seek(0, SeekOrigin.Begin);
					await memStream.CopyToAsync(stream);
					await response.Content.CopyToAsync(stream);

//				Console.WriteLine("Step2: "+Thread.CurrentThread.ManagedThreadId);
//				await stream.WriteAsync(content, 0, content.Length);
//				Console.WriteLine("Step3: "+Thread.CurrentThread.ManagedThreadId);

//				}
				}
				stream.Close();


//				byte[] buffer = new byte[4096];

//				socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
//				socket.Send(content);
//				socket.Close();

				buffers.Release(bufferNode);
				netStreams.Release(streamNode);
//				httpClients.Release(clientNode);
//				});

//				SocketAsyncEventArgs readEventArgs = buffers.Get();
//				if(readEventArgs == null) {
//					readEventArgs = new SocketAsyncEventArgs();
//					byte[] buffer = new byte[4096];
//					readEventArgs.SetBuffer(buffer, 0, buffer.Length);
//					readEventArgs.Completed += ReceiveCompleted;
//				}
//				readEventArgs.UserToken = socket;
//				if(!socket.ReceiveAsync(readEventArgs))
//					ReceiveCompleted(null, readEventArgs);

			}
		}

/*		void ReceiveCompleted(object sender, SocketAsyncEventArgs readEventArgs)
		{
			SocketAsyncEventArgs writeEventArgs = new SocketAsyncEventArgs();
			writeEventArgs.SetBuffer(content, 0, content.Length);
			writeEventArgs.Completed += SendCompleted;
			writeEventArgs.UserToken = readEventArgs.UserToken;

			if(!((Socket)readEventArgs.UserToken).SendAsync(writeEventArgs))
				SendCompleted(sender, writeEventArgs);

			buffers.Release(readEventArgs);
		}

		void SendCompleted(object sender, SocketAsyncEventArgs writeEventArgs)
		{
			((Socket)writeEventArgs.UserToken).Close();
		}*/

		public void Stop()
		{
			listener.Close();
		}
	}

	class MainClass
	{
		public static TaskFactory TestFactory = new TaskFactory(new TestScheduler());

		public static void Main(string[] args)
		{
			Server server = new Server(3333);
			server.Start();
			Console.ReadLine();
			server.Stop();
		}
	}
}
