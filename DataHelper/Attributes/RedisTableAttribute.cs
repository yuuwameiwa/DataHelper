namespace DataHelper.Attributes
{
    public class RedisTableAttribute : Attribute
    {
        public string Key;
        public string Identity;
        public int Expire_s;

        public RedisTableAttribute(string key, string identity, int expire_s)
        {
            Key = key;
            Identity = identity;
            Expire_s = expire_s;
        }
    }
}
