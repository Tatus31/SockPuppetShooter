using Player;
using Utils;

namespace Managers {
    public class GameManager : BetterSingleton<GameManager> {
        public DebugLogManager debugLogManager;
        public PlayerInputManager playerInputManager;
        public LayerManager layerManager;

        void Start() {
            debugLogManager = DebugLogManager.TryGetInstance();
            playerInputManager = PlayerInputManager.TryGetInstance();
            layerManager = LayerManager.TryGetInstance();
        }
    }
}
