//泛型单例类
public class Singleton<T>
    where T : new()
{
    private static T Instance;

    public static T GetSinglton()
    {
        if (Instance == null)
            Instance = new T();

        return Instance;
    }

    public virtual void Init(params object[] paramList) { }
}
