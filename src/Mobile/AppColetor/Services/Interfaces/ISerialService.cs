namespace AppColetor.Services.Interfaces
{
    public interface ISerialService
    {
        event EventHandler<SerialDataEventArgs>? DataReceived;
        event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
        event EventHandler<SerialErrorEventArgs>? ErrorOccurred;

        bool IsConnected { get; }
        string? DeviceName { get; }

        Task<List<SerialDeviceInfo>> ListarDispositivosAsync();
        Task<bool> SolicitarPermissaoAsync(SerialDeviceInfo device);
        Task<bool> ConectarAsync(SerialDeviceInfo device, SerialConfig config);
        Task DesconectarAsync();
        Task EnviarAsync(string dados);
        Task EnviarAsync(byte[] dados);
    }

    public class SerialDeviceInfo
    {
        public string DeviceId { get; set; } = "";
        public string Name { get; set; } = "";
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public string ChipType { get; set; } = "";
        public bool HasPermission { get; set; }

        public string DisplayName => $"{Name} ({ChipType})";
    }

    public class SerialConfig
    {
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public int ReadTimeout { get; set; } = 1000;
        public string LineEnding { get; set; } = "\r\n";
    }

    public enum Parity { None, Even, Odd, Mark, Space }
    public enum StopBits { One, OnePointFive, Two }

    public class SerialDataEventArgs : EventArgs
    {
        public string Data { get; set; } = "";
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; set; }
        public string? DeviceName { get; set; }
        public string? Message { get; set; }
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    public class SerialErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
        public bool IsFatal { get; set; }
    }
}