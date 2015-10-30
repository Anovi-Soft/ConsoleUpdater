namespace ConsoleUpdaterServer.Workers
{
    interface IWorker
    {

        void Init(object arg = null);

        void Update(bool hard, string[] args);
    }
}
