using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class HandInput : MonoBehaviour
{
    public int leftFingers;
    public int rightFingers;

    UdpClient client;
    int port = 5052;

    void Start()
    {
        client = new UdpClient(port);
        client.BeginReceive(ReceiveData, null);
    }

    void ReceiveData(System.IAsyncResult result)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        byte[] data = client.EndReceive(result, ref ep);

        string message = Encoding.UTF8.GetString(data);
        string[] values = message.Split(',');

        if (values.Length == 2)
        {
            int.TryParse(values[0], out leftFingers);
            int.TryParse(values[1], out rightFingers);
        }

        client.BeginReceive(ReceiveData, null);
    }
}