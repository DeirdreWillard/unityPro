public class HotfixComponent
    {
        protected bool m_IsInitialized = false;
        protected bool m_IsDisposed = false;

        public virtual void Initialize()
        {
            m_IsInitialized = true;
            m_IsDisposed = false;
        }

        public virtual void InitializeOnEnterGame()
        {

        }

        public virtual void Update(float deltaTime)
        {

        }

        public virtual void Dispose()
        {
            m_IsDisposed = true;
        }

        public virtual void Shutdown()
        {

        }
    }


