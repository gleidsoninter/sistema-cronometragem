namespace AppColetor.Helpers
{
    public static class Constants
    {
        // ═══════════════════════════════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════════════════════════════

        public const string API_BASE_URL_DEFAULT = "http://192.168.1.100:5000";
        public const string API_VERSION = "v1";
        public const int API_TIMEOUT_SECONDS = 30;
        public const int API_RETRY_COUNT = 3;

        // ═══════════════════════════════════════════════════════════════════
        // SERIAL
        // ═══════════════════════════════════════════════════════════════════

        public const int BAUD_RATE_DEFAULT = 115200;
        public const int DATA_BITS_DEFAULT = 8;
        public const int READ_TIMEOUT_MS = 1000;
        public const int WRITE_TIMEOUT_MS = 1000;
        public const string LINE_ENDING_DEFAULT = "\r\n";

        // Baud rates suportados
        public static readonly int[] BAUD_RATES = { 9600, 19200, 38400, 57600, 115200 };

        // ═══════════════════════════════════════════════════════════════════
        // DATABASE
        // ═══════════════════════════════════════════════════════════════════

        public const string DATABASE_NAME = "coletor.db3";
        public const SQLite.SQLiteOpenFlags DATABASE_FLAGS =
            SQLite.SQLiteOpenFlags.ReadWrite |
            SQLite.SQLiteOpenFlags.Create |
            SQLite.SQLiteOpenFlags.SharedCache;

        // ═══════════════════════════════════════════════════════════════════
        // APP
        // ═══════════════════════════════════════════════════════════════════

        public const int MAX_LEITURAS_EM_MEMORIA = 100;
        public const int INTERVALO_SYNC_SEGUNDOS = 5;
        public const int INTERVALO_HEARTBEAT_SEGUNDOS = 30;

        // ═══════════════════════════════════════════════════════════════════
        // STORAGE KEYS
        // ═══════════════════════════════════════════════════════════════════

        public const string KEY_API_URL = "api_url";
        public const string KEY_API_TOKEN = "api_token";
        public const string KEY_DEVICE_ID = "device_id";
        public const string KEY_BAUD_RATE = "baud_rate";
        public const string KEY_ID_ETAPA = "id_etapa";
        public const string KEY_PROTOCOLO = "protocolo";
        public const string KEY_ULTIMO_SYNC = "ultimo_sync";

        // ═══════════════════════════════════════════════════════════════════
        // PROTOCOLOS SUPORTADOS
        // ═══════════════════════════════════════════════════════════════════

        public const string PROTOCOLO_GENERICO = "GENERICO";      // NUMERO,TIMESTAMP
        public const string PROTOCOLO_RF_TIMING = "RF_TIMING";    // #MOTO:TEMPO:VOLTA#
        public const string PROTOCOLO_AMB = "AMB";                // @T:ID:LOOP:TIME:HITS

        public static readonly string[] PROTOCOLOS = { PROTOCOLO_GENERICO, PROTOCOLO_RF_TIMING, PROTOCOLO_AMB };
    }
}