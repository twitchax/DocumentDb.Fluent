using System.Threading.Tasks;

namespace DocumentDb.Fluent
{
    public static class Helpers
    {
        public static void Synchronize(Task t)
        {
            t.Wait();
        }

        public static TOut Synchronize<TOut>(Task<TOut> t)
        {
            t.Wait();
            return t.Result;
        }
    }
}
