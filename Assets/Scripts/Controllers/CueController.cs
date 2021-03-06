using System.Collections;
using BilliardGame.Managers;
using BilliardGame.EventHandlers;
using UnityEngine;

namespace BilliardGame.Controllers
{
    class CueController : MonoBehaviour
    {
        [SerializeField]
        private Transform _cueBall = null;

        private float _defaultDistFromCueBall;

        private float _maxClampDist = 9;

        private float _forceGathered = 0.0f;

        private float _forceThreshold = 0.5f;

        private float _speed = 10.0f;
        private bool _cueReleasedToStrike = false;

        private Vector3 _initialPos;
        private Vector3 _initialDir;
        AudioSource audioSource ;

        private Vector3 _posToRot = Vector3.one;

        public float ForceGatheredToHit { get { return _forceGathered;  } }

        private void Start()
        {
            _initialPos = transform.position;
            _initialDir = transform.forward;
            _defaultDistFromCueBall = Vector3.Distance(_cueBall.position, transform.position);

            EventManager.Subscribe(typeof(GameInputEvent).Name, OnGameInputEvent);
            EventManager.Subscribe(typeof(CueBallActionEvent).Name, OnCueBallEvent);
            EventManager.Subscribe(typeof(GameStateEvent).Name, OnGameStateEvent);
        }

        private void OnDestroy()
        {
            EventManager.Unsubscribe(typeof(GameInputEvent).Name, OnGameInputEvent);
            EventManager.Unsubscribe(typeof(CueBallActionEvent).Name, OnCueBallEvent);
            EventManager.Unsubscribe(typeof(GameStateEvent).Name, OnGameStateEvent);
        }

        private void OnGameInputEvent(object sender, IGameEvent gameEvent)
        {
            GameInputEvent gameInputEvent = (GameInputEvent)gameEvent;
            switch (gameInputEvent.State)
            {
                case GameInputEvent.States.HorizontalAxisMovement:
                    {
                        if (_posToRot == Vector3.one)
                            transform.RotateAround(_cueBall.position, Vector3.up, 20f * gameInputEvent.axisOffset * Time.deltaTime);
                        else
                            transform.RotateAround(_posToRot, Vector3.up, 20f * gameInputEvent.axisOffset * Time.deltaTime);
                    }
                    break;
                case GameInputEvent.States.VerticalAxisMovement:
                    {
                        if (_posToRot != Vector3.one)
                            return;

                        var newPosition = transform.position + transform.forward * gameInputEvent.axisOffset;

                        _forceGathered = Vector3.Distance(_cueBall.position, newPosition);
                        if ((_forceGathered < _defaultDistFromCueBall + _maxClampDist) &&
                            _forceGathered > _defaultDistFromCueBall)
                        {
                            transform.position = newPosition;
                            EventManager.Notify(typeof(CueActionEvent).ToString(), this, new CueActionEvent() { ForceGathered = _forceGathered });
                        }
                        else
                        {
                            // else
                        }

                    }
                    break;
                case GameInputEvent.States.Release:
                    {
                        
                        if (_posToRot != Vector3.one)
                            return;

                        if (_forceGathered > _defaultDistFromCueBall + _forceThreshold)
                            _cueReleasedToStrike = true;
                            
                    }
                    break;
            }
        }

       
        /// <param name="sender"></param>
        /// <param name="gameEvent"></param>
        private void OnCueBallEvent(object sender, IGameEvent gameEvent)
        {
            CueBallActionEvent cueBallActionEvent = (CueBallActionEvent)gameEvent;

            switch (cueBallActionEvent.State)
            {
                case CueBallActionEvent.States.Stationary:
                case CueBallActionEvent.States.Default:
                    {
                        _forceGathered = 0f;

                        transform.position = _cueBall.transform.position - transform.forward * _defaultDistFromCueBall;
                        transform.LookAt(_cueBall);

                        _posToRot = Vector3.one;
                    }
                    break;
                case CueBallActionEvent.States.Striked:
                    {
                        _cueReleasedToStrike = false;

                        if (GameManager.Instance.CurrGameState == GameManager.GameState.Play)
                        {
                            StartCoroutine(MoveCueAfterStrike(transform.position, _cueBall.transform.position - transform.forward * _defaultDistFromCueBall * 1.5f, 1.0f));
                        }

                        transform.LookAt(_cueBall);

                        _posToRot = _cueBall.transform.position;
                    }
                    break;
            }
        }

        private void OnGameStateEvent(object sender, IGameEvent gameEvent)
        {
            GameStateEvent gameStateEvent = (GameStateEvent)gameEvent;
            switch(gameStateEvent.GameState)
            {
                case GameStateEvent.State.Play:
                    {
                        PlaceInInitialPosAndRot();
                    }
                    break;
            }
        }

       
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="overTime"></param>
        /// <returns></returns>
        IEnumerator MoveCueAfterStrike(Vector3 source, Vector3 target, float overTime)
        {
            float startTime = Time.time;
            while (Time.time < startTime + overTime)
            {
                transform.position = Vector3.Lerp(source, target, (Time.time - startTime) / overTime);
                yield return null;
            }
            transform.position = target;
        }

        private void FixedUpdate()
        {
            if(_cueReleasedToStrike)
            {
                float step = _speed * Time.deltaTime * (_forceGathered/_speed);
                transform.position = Vector3.MoveTowards(transform.position, _cueBall.transform.position, step);
                
                audioSource = GetComponent<AudioSource>();
                audioSource.Play();
            }
        }

        private void PlaceInInitialPosAndRot()
        {
            _forceGathered = 0f;
            _cueReleasedToStrike = false;
            _posToRot = Vector3.one;

            transform.position = _initialPos;
            transform.forward = _initialDir;
        }
    }
}
