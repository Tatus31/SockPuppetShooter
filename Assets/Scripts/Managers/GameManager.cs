using Player;
using Utils;

namespace Managers {
    public class GameManager : BetterSingleton<GameManager> {
        public DebugLogManager debugLogManager;
        public PlayerInputManager playerInputManager;

        void Start() {
            debugLogManager = DebugLogManager.TryGetInstance();
            playerInputManager = PlayerInputManager.TryGetInstance();
        }
    }
}
