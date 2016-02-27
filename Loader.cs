using ICities;

namespace SkylinesOverwatch
{
    public class Loader : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            Helper.Instance.GameLoaded = loading.loadingComplete;
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
                Helper.Instance.GameLoaded = true;
        }

        public override void OnLevelUnloading()
        {
            Helper.Instance.GameLoaded = false;
        }
    }
}