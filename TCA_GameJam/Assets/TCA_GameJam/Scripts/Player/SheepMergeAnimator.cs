using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TCA_GameJam.Scripts.Player
{
    /// <summary>
    /// 合体のとき、仲間が先頭の羊に一気に集まって、大きな毛玉がポンッと出てくる
    /// </summary>
    [RequireComponent(typeof(SheepHerd))]
    public class SheepMergeAnimator : MonoBehaviour
    {
        [SerializeField] private GameObject _followerPrefab;        // 集まってくる仲間の見た目(SheepHerd と同じ Prefab)
        [SerializeField] private Transform _woolBall;               // SheepHerd と同じ毛玉

        [Header("集まる")]
        [SerializeField] private float _gatherDuration = 0.4f;
        [SerializeField] private float _gatherDelayRandom = 0.1f;   // 1匹ずつ少しずらして集まる
        [SerializeField] private Ease _gatherEase = Ease.InBack;

        [Header("毛玉が出る")]
        [SerializeField] private float _popDuration = 0.4f;
        [SerializeField] private Ease _popEase = Ease.OutBack;

        // 0 にすると SheepHerd の転がす計算(半径で割る)が壊れるので、ごく小さい大きさから始める
        private const float POP_START_SCALE_RATE = 0.01f;

        private SheepHerd _sheepHerd;
        private Sequence _popSequence;

        private void Awake()
        {
            _sheepHerd = GetComponent<SheepHerd>();
        }

        private void OnEnable()
        {
            _sheepHerd.OnMerged += HandleMerged;
        }

        private void OnDisable()
        {
            _sheepHerd.OnMerged -= HandleMerged;
        }

        private void OnDestroy()
        {
            _popSequence?.Kill();
        }

        private void HandleMerged(IReadOnlyList<Vector3> followerLocalPositions)
        {
            foreach (Vector3 localPosition in followerLocalPositions)
            {
                GatherFollower(localPosition);
            }

            PopWoolBall(followerLocalPositions.Count > 0);
        }

        /// <summary>
        /// 仲間がいた場所に見た目だけ出して、先頭の羊の位置へ縮みながら集める
        /// </summary>
        private void GatherFollower(Vector3 localPosition)
        {
            GameObject follower = Instantiate(_followerPrefab, transform);
            Transform followerTransform = follower.transform;
            followerTransform.localPosition = localPosition;

            float delay = Random.Range(0.0f, _gatherDelayRandom);
            DOTween.Sequence()
                .SetDelay(delay)
                .Join(followerTransform.DOLocalMove(Vector3.zero, _gatherDuration).SetEase(_gatherEase))
                .Join(followerTransform.DOScale(Vector3.zero, _gatherDuration).SetEase(_gatherEase))
                .SetLink(follower)
                .OnComplete(() => Destroy(follower));
        }

        /// <summary>
        /// 仲間が集まり終わったら、毛玉を小さい所から本来の大きさまでポンッと大きくする
        /// </summary>
        private void PopWoolBall(bool hasFollowers)
        {
            // SheepHerd が決めた大きさが本来の大きさ
            Vector3 targetScale = _woolBall.localScale;
            float waitTime = hasFollowers ? _gatherDuration + _gatherDelayRandom : 0.0f;

            _popSequence?.Kill();
            _woolBall.localScale = targetScale * POP_START_SCALE_RATE;
            _popSequence = DOTween.Sequence()
                .AppendInterval(waitTime)
                .Append(_woolBall.DOScale(targetScale, _popDuration).SetEase(_popEase))
                .SetLink(_woolBall.gameObject);
        }
    }
}
