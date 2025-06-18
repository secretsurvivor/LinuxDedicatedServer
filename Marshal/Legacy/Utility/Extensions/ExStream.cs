namespace LinuxDedicatedServer.Legacy.Utility.Extensions
{
    public static class ExStream
    {
        public static StreamReader AsReader(this Stream stream)
        {
            return new StreamReader(stream, leaveOpen: true);
        }

        public static StreamWriter AsWriter(this Stream stream)
        {
            return new StreamWriter(stream, leaveOpen: true);
        }
    }
}
