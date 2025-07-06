  using System.Collections.Generic;

using UnityEngine;

namespace Assets.Scripts.game.sfx
{
    /// <summary>
    /// container for ghosting sprites. triggers a new ghosting object over a set amount of time, referencing a sprite renderer's current sprite
    /// combined, this forms a trailing effect behind the player 
    /// </summary>
    public class GhostingContainer : MonoBehaviour
    {
        private List<GhostingSprite> _inactiveGhostingSpritesPool; //a list of sprites stored in memory
//
      //  [SerializeField]
       // private int _spacing;

        [SerializeField] private float _spawnRate;
        private float _nextSpawnTime;
        [SerializeField]
        private int _trailLength=100; //effect duration

       // private int _frameCount;
        [SerializeField]
        private float _desiredAlpha = 0.8f;
        private Queue<GhostingSprite> _ghostingSpritesQueue; 
        private bool _hasStarted;
        //private float _triggerTimer = 0.2f; // how often to trigger a ghosting image?
        [SerializeField]
        private float _effectDuration = 1f;
        //collection of all sprite renderers that should generate a ghost each spawn
        private List<SpriteRenderer> _refSpriteRenderers;
        [SerializeField] private Color _desiredColor;
        private GameObject _ghostSpritesParent;
		private bool useTint;
        /// <summary>
        /// Average ms per frame
        /// </summary>
        
        public List<GhostingSprite> InactiveGhostSpritePool
        {
            get
            {
                if (_inactiveGhostingSpritesPool == null)
                {
                    _inactiveGhostingSpritesPool = new List<GhostingSprite>(5);
                }
                return _inactiveGhostingSpritesPool;
            }
            set { _inactiveGhostingSpritesPool = value; }
        }

        public Queue<GhostingSprite> GhostingSpritesQueue
        {
            get{
             if (_ghostingSpritesQueue == null)
                {
                    _ghostingSpritesQueue = new Queue<GhostingSprite>(_trailLength);
                }
                return _ghostingSpritesQueue;
            } //idito
            set { _ghostingSpritesQueue = value; }
    
        }

        public GameObject GhostSpritesParent
        {
            get
            {
                if (_ghostSpritesParent == null)
                {
                    _ghostSpritesParent = new GameObject();
                    _ghostSpritesParent.transform.position = Vector3.zero;
                    _ghostSpritesParent.name = "GhostspriteParent";
                }
                return _ghostSpritesParent;
            }
            set { _ghostSpritesParent = value; }
        }


        /// <summary>
        /// Initializes and starts the ghosting effect with the given params but with an option to tint. Please note that the effect duration no longer has an effect on the object in question.  
        /// </summary>
        /// <param name="maxNumberOfGhosts"></param>
        /// <param name="spawnRate"></param>
        /// <param name="desiredColor"></param>
        public void Init(int maxNumberOfGhosts, float spawnRate, Color desiredColor)
        {
            _desiredColor = desiredColor;
            _trailLength = maxNumberOfGhosts;
           // _spacing = spacing;
            _spawnRate=spawnRate;
            //_effectDuration = effectDuration;
			_effectDuration = maxNumberOfGhosts * spawnRate * 0.95f; //5% variance
            //collect every SpriteRenderer in the hierarchy starting from this object
            _refSpriteRenderers = new List<SpriteRenderer>(transform.GetComponentsInChildren<SpriteRenderer>(false));
            if (_refSpriteRenderers.Count == 0)
            {
                Debug.LogWarning("GhostingContainer.Init: No SpriteRenderers found under this transform");
            }
            _nextSpawnTime = Time.time + _spawnRate;
			_hasStarted = true;
			useTint = true;
          }
        /// <summary>
		/// Initializes and starts the ghosting effect with the given params. Please note that the effect duration no longer has an effect on the object in question.
        /// </summary>
        /// <param name="maxNumberOfGhosts"></param>
        /// <param name="spawnRate"></param>
        public void Init(int maxNumberOfGhosts, float spawnRate)
        { 
            _trailLength = maxNumberOfGhosts;
            _spawnRate = spawnRate; 
		//	_effectDuration = effectDuration;
			_effectDuration = maxNumberOfGhosts * spawnRate * 0.95f; //5% variance
            //collect every SpriteRenderer in the hierarchy starting from this object
            _refSpriteRenderers = new List<SpriteRenderer>(transform.GetComponentsInChildren<SpriteRenderer>(false));
            _nextSpawnTime = Time.time + _spawnRate;
			useTint = false;
            _hasStarted = true;
        }
        /// <summary>
        /// Stop the ghosting effect
        /// </summary>
        public void StopEffect()
        {
            _hasStarted = false;
        }

        void Update()
        {
            if (_hasStarted)
            { 
                //calculate maximum ghosts allowed in queue for this frame
                int totalCap = _trailLength * (_refSpriteRenderers != null && _refSpriteRenderers.Count > 0 ? _refSpriteRenderers.Count : 1);

                //check for spawn rate
               //check if we're ok to spawn a new ghost
                if (Time.time >=_nextSpawnTime )
                {  
                    //loop through all reference renderers and attempt to spawn/reuse ghosts for each of them
                    foreach (var sr in _refSpriteRenderers)
                    {
                        GhostingSprite ghostSprite;

                        if (GhostingSpritesQueue.Count < totalCap)
                        {
                            //still room – get a (possibly new) sprite and enqueue
                            ghostSprite = Get();
                            GhostingSpritesQueue.Enqueue(ghostSprite);
                        }
                        else
                        {
                            //queue is full – try reuse first element
                            ghostSprite = GhostingSpritesQueue.Peek();
                            if (!ghostSprite.CanBeReused())
                            {
                                //cannot reuse yet, skip this renderer this frame
                                continue;
                            }

                            GhostingSpritesQueue.Dequeue();
                            GhostingSpritesQueue.Enqueue(ghostSprite);
                        }

                        //initialise sprite
                        if (!useTint)
                        {
                            ghostSprite.Init(_effectDuration, _desiredAlpha, sr.sprite, sr.sortingLayerID, sr.sortingOrder - 1, sr.transform, Vector3.zero);
                        }
                        else
                        {
                            ghostSprite.Init(_effectDuration, _desiredAlpha, sr.sprite, sr.sortingLayerID, sr.sortingOrder - 1, sr.transform, Vector3.zero, _desiredColor);
                        }
                    }

                    _nextSpawnTime += _spawnRate; 
 
                }
                //check if the queue count is greater than the trail length. Dequeue these items off the queue, as they are no longer needed
                if (GhostingSpritesQueue.Count > totalCap)
                { 
                    int difference = GhostingSpritesQueue.Count - totalCap;
                    for (int i = 1; i < difference; i++)
                    {
                      GhostingSprite gs = GhostingSpritesQueue.Dequeue();
                        InactiveGhostSpritePool.Add(gs);
                    }
                    return;
                }
            }

           
         
        }



    

        /// <summary>
        /// Returns a ghosting sprite 
        /// </summary>
        /// <returns></returns>
        private GhostingSprite Get()
        {

            for (int i = 0; i < InactiveGhostSpritePool.Count; i++)
            {
                if (InactiveGhostSpritePool[i].CanBeReused())
                {
                    return InactiveGhostSpritePool[i];
                }

            }
            return BuildNewGhostingSprite();


        }

        private GhostingSprite BuildNewGhostingSprite()
        {
            //create a gameobject and set the current transform as a parent
            GameObject go = new GameObject();
            go.transform.position = transform.position;
            go.transform.parent = GhostSpritesParent.transform;
    
            GhostingSprite gs = go.AddComponent<GhostingSprite>();
        
            return gs;
        }

    }
}
