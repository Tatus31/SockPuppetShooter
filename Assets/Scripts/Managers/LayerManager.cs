using UnityEngine;
using Utils;

namespace Managers {
    public class LayerManager : BetterSingleton<LayerManager> {
        [SerializeField] LayerMask groundCollisionIgnoredLayer;
        [SerializeField] LayerMask damageableEntityLayerMask;
        
        public int GetGroundedCastLayerMask => ~groundCollisionIgnoredLayer;
        
        public int GetDamageableEntityLayerMask => damageableEntityLayerMask;
    }
}
