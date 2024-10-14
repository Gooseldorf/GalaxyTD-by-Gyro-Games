using CardTD.Utilities;
using DG.Tweening;
using ECSTest.Components;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
using static MusicManager;

namespace UI
{
    public class EnergyCoreWidget : ClickableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<EnergyCoreWidget> { }

        private VisualElement coreIcon;
        private VisualElement cornerFrame;

        private VisualElement topLeftFrame;
        private VisualElement topRightFrame;
        private VisualElement botLeftFrame;
        private VisualElement botRightFrame;

        private VisualElement progressBarFilling;
        private VisualElement progressBarBackground;
        private VisualElement progressBarContainer;
        private List<VisualElement> powerCells;
        private Entity energyCore;
        private Label cellsCount;
        private VisualElement cellsCounterContainer;
        private VisualElement alertIcon;
        private VisualElement dangerIcon;

        private int startCellsCount = 0;

        private int detachedCells = 0;
        private int destroyedCells = 0;

        private float energyCoreDeactivationTime;
        private Tween progressBarTween;
        private bool haveProgressBar;

        public Entity EnergyCore => energyCore;
        private VisualTreeAsset powerCellPrefab;

        private const string LOST_KEY = "EnergyCellIcon_lost";
        private const string TAKEN_KEY = "EnergyCellIcon_taken";
        private const string ACTIVE_KEY = "EnergyCellIcon";

        private bool hasCells => powerCells.Count - detachedCells > 0;

        public void Init(Entity core, VisualTreeAsset powerCellPrefab)
        {
            base.Init();
            this.powerCellPrefab = powerCellPrefab;
            energyCore = core;
            powerCells = new();
            coreIcon = this.Q<VisualElement>("CoreIcon");
            cornerFrame = this.Q<VisualElement>("CornersFrame");

            topLeftFrame = this.Q("Top-Left");
            topRightFrame = this.Q("Top-Right");
            botLeftFrame = this.Q("Down-Left");
            botRightFrame = this.Q("Down-Right");

            progressBarFilling = this.Q<VisualElement>("ProgressBarFilling");
            progressBarBackground = this.Q<VisualElement>("ProgressBarBackground");
            progressBarContainer = this.Q<VisualElement>("ProgressBarContainer");
            cellsCount = this.Q<Label>("CellCounter");
            cellsCounterContainer = this.Q<VisualElement>("CounterContainer");
            alertIcon = this.Q("AlertIcon");
            dangerIcon = this.Q("DangerIcon");

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EnergyCoreComponent corComp = manager.GetComponentData<EnergyCoreComponent>(core);
            startCellsCount = corComp.PowerCellCount;
            energyCoreDeactivationTime = corComp.DeactivationTime;

            haveProgressBar = energyCoreDeactivationTime > 0;

            if (haveProgressBar)
                progressBarFilling.style.width = new StyleLength(Length.Percent(0));
            else
                progressBarContainer.style.display = DisplayStyle.None;

            for (int i = 0; i < startCellsCount; i++)
                AddPowerCell();
            scale = 1;
            //DOVirtual.DelayedCall(1f, StartProgressBar);
            alertAlpha = 0;
            dangerIcon.style.visibility = Visibility.Hidden;
            //dangerAlpha = 0;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            cellsCount.text = $"{powerCells.Count - detachedCells}";

            if (float.IsNaN(coreIcon.resolvedStyle.width))
            {
                elementsToResolve.Add(coreIcon);
                coreIcon.RegisterCallback<GeometryChangedEvent>(CheckElementsResolvedAndArrange);
                return;
            }
            ArrangeElementsInCircle(powerCells, coreIcon.resolvedStyle.width / 2, .5f, true, -90);
            CheckShowDanger();
        }

        private void CheckShowDanger()
        {
            //if (powerCells.Count - detachedCells <= powerCells.Count * .2f && dangerSeq == null)
            //    ShowDanger();
            //if (powerCells.Count - detachedCells > powerCells.Count * .2f && dangerSeq != null)
            //    HideDanger();
            if (powerCells.Count - detachedCells <= powerCells.Count * .2f && dangerIcon.style.visibility == Visibility.Hidden)
                dangerIcon.style.visibility = Visibility.Visible;
            if (powerCells.Count - detachedCells > powerCells.Count * .2f && dangerIcon.style.visibility == Visibility.Visible)
                dangerIcon.style.visibility = Visibility.Hidden;
        }

        public void ActivateAlert(bool activate)
        {
            if (!hasCells)
            {
                HideAlert();
                return;
            }

            if (activate && alertSeq == null)
                ShowAlert();
            if (!activate && alertSeq != null)
                HideAlert();
        }

        private List<VisualElement> elementsToResolve = new();

        private void CheckElementsResolvedAndArrange(GeometryChangedEvent geom)
        {
            ((VisualElement)geom.currentTarget).UnregisterCallback<GeometryChangedEvent>(CheckElementsResolvedAndArrange);
            elementsToResolve.Remove(((VisualElement)geom.currentTarget));
            if (elementsToResolve.Count == 0)
            {
                ArrangeElementsInCircle(powerCells, coreIcon.resolvedStyle.width / 2, .5f, true, -90);
                Messenger.Broadcast(UIEvents.OnElementResolved, MessengerMode.DONT_REQUIRE_LISTENER);
            }
        }

        private void AddPowerCell()
        {
            VisualElement powerCell = powerCellPrefab.Instantiate().Q<VisualElement>("PowerCell");
            this.Q<VisualElement>("Center").Add(powerCell);
            powerCells.Add(powerCell);
            elementsToResolve.Add(powerCell);
            powerCell.RegisterCallback<GeometryChangedEvent>(CheckElementsResolvedAndArrange);
        }

        public void StartProgressBar()
        {
            if (!haveProgressBar) return;

            progressBarTween = UIHelper.Instance.ChangeWidthByPercent(progressBarFilling, 100, 0, energyCoreDeactivationTime);
            progressBarTween.OnComplete(() => progressBarTween = null);
            progressBarTween.Play();
        }

        public void Reset()
        {
            ToggleCore(true);

            this.Q<VisualElement>("Center").Clear();
            powerCells.Clear();

            for (int i = 0; i < startCellsCount; i++)
                AddPowerCell();

            if (haveProgressBar)
            {
                if (progressBarTween != null)
                    progressBarTween.Kill();
                progressBarFilling.style.width = new StyleLength(Length.Percent(0));
            }

            ToggleCore(true);

            foreach (var cell in powerCells)
                cell.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite("EnergyCellIcon"));

            destroyedCells = 0;
            detachedCells = 0;
            cellsCount.text = $"{powerCells.Count - detachedCells}";

            scale = 1;
            frameColor = normalColor;

            CheckShowDanger();
        }

        public void DestroyCell()
        {
            if (destroyedCells < powerCells.Count && destroyedCells >= 0)
            {
                powerCells[destroyedCells].style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite(LOST_KEY));
                destroyedCells++;
            }

            UpdateVisual();
        }

        public void AddPowerCells(PowerCellEvent powerCellEvent)
        {
            for (int i = 0; i < powerCellEvent.Value; i++)
            {
                AddPowerCell();
            }
            UpdateVisual();
        }


        public void DestroyAll()
        {
            for (int i = 0; i < powerCells.Count; i++)
            {
                powerCells[i].style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite(LOST_KEY));
            }

            destroyedCells = powerCells.Count;

            ToggleCore(false);
        }

        public void DetachCell()
        {
            if (detachedCells < 0 || detachedCells >= powerCells.Count)
            {
                Debug.LogWarning($"detachedCells {detachedCells}");
                return;
            }
            ShowScale();
            powerCells[detachedCells].style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite(TAKEN_KEY));
            detachedCells++;

            CheckShowDanger();
            UpdateVisual();
            if(powerCells.Count - detachedCells < 5) MusicManager.PlaySound2D(SoundKey.Cell_lowInCore);
            if (!hasCells)
            {
                MusicManager.PlaySound2D(SoundKey.Core_lost);
                ToggleCore(false);
            }
        }

        public void ReturnCell()
        {
            if (detachedCells <= 0)
                AddPowerCell();
            else
            {
                detachedCells--;
                powerCells[detachedCells].style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite(ACTIVE_KEY));
            }

            CheckShowDanger();
            UpdateVisual();
        }

        private void ToggleCore(bool isActive)
        {
            if (!isActive)
            {
                if (haveProgressBar)
                {
                    if (progressBarTween != null)
                        progressBarTween.Pause();
                    progressBarContainer.style.display = DisplayStyle.None;
                }
                coreIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite("EnergyCoreIconLost"));
                cornerFrame.style.display = DisplayStyle.None;
                cellsCounterContainer.style.display = DisplayStyle.None;
                dangerIcon.style.visibility = Visibility.Hidden;
                //HideAlert();
            }
            else
            {
                coreIcon.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite("EnergyCoreIcon"));
                cornerFrame.style.display = DisplayStyle.Flex;
                cellsCounterContainer.style.display = DisplayStyle.Flex;
                if (haveProgressBar)
                {
                    progressBarContainer.style.display = DisplayStyle.Flex;
                    if (progressBarTween != null)
                        progressBarTween.Play();
                }
            }
        }

        public List<VisualElement> GetActiveCellsForAnimation()
        {
            List<VisualElement> activeCellClones = new();
            foreach (var powerCell in powerCells)
                powerCell.style.backgroundImage = new StyleBackground(UIHelper.Instance.GetEnergyCellSprite(LOST_KEY));

            for (int i = detachedCells; i < powerCells.Count; i++)
            {
                VisualElement powerCell = powerCellPrefab.Instantiate().Q<VisualElement>("PowerCell");
                this.Q<VisualElement>("Center").Add(powerCell);
                powerCell.style.position = Position.Absolute;
                powerCell.style.left = powerCells[i].style.left;
                powerCell.style.top = powerCells[i].style.top;
                powerCell.style.right = powerCells[i].style.right;
                powerCell.style.bottom = powerCells[i].style.bottom;

                activeCellClones.Add(powerCell);
            }

            return activeCellClones;
        }

        private void ArrangeElementsInCircle(List<VisualElement> elements, float coreRadius, float offset, bool clockwise = true, float startAngle = 0)
        {
            int maxElementsPerCircle = Mathf.CeilToInt((float)elements.Count / 2);
            startAngle *= Mathf.Deg2Rad;
            float elementRadius = elements[0].resolvedStyle.width / 2;
            float circleRadius = coreRadius + offset;
            float step = clockwise ? 2 * Mathf.PI / maxElementsPerCircle : -2 * Mathf.PI / maxElementsPerCircle;

            for (int i = 0; i < elements.Count; i++)
            {
                float angle = startAngle + (step * (i % maxElementsPerCircle));

                Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;
                elements[i].style.left = position.x - elementRadius;
                elements[i].style.right = position.x + elementRadius;
                elements[i].style.top = position.y - elementRadius;
                elements[i].style.bottom = position.y + elementRadius;
                elements[i].style.position = Position.Absolute;

                if (i == (maxElementsPerCircle - 1))
                {
                    circleRadius += elementRadius;
                    startAngle += step / 2;
                    step = clockwise ? 2 * Mathf.PI / (elements.Count - maxElementsPerCircle) : -2 * Mathf.PI / (elements.Count - maxElementsPerCircle);
                }
            }
        }

        private void ShowAlert()
        {
            if (alertSeq != null)
                alertSeq.Kill();
            alertSeq = DOTween.Sequence();
            alertAlpha = 0;
            alertSeq.Append(DOTween.To(() => alertAlpha, x => alertAlpha = x, 1, 1f));
            alertSeq.Append(DOTween.To(() => alertAlpha, x => alertAlpha = x, 0, .5f));
            alertSeq.SetLoops(-1);
        }

        private void HideAlert()
        {
            if (alertSeq != null)
            {
                alertSeq.Kill();
                alertSeq = null;
                alertAlpha = 0;
            }
        }

        //private void ShowDanger()
        //{
        //    if (dangerSeq != null)
        //        dangerSeq.Kill();
        //    dangerSeq = DOTween.Sequence();
        //    dangerAlpha = 0;
        //    dangerSeq.Append(DOTween.To(() => dangerAlpha, x => dangerAlpha = x, 1, 1f));
        //    dangerSeq.Append(DOTween.To(() => dangerAlpha, x => dangerAlpha = x, 0, .5f));
        //    dangerSeq.SetLoops(-1);
        //}

        //private void HideDanger()
        //{
        //    if (dangerSeq != null)
        //        dangerSeq.Kill();
        //    dangerSeq = null;
        //    dangerAlpha = 0;
        //}

        private void ShowScale()
        {
            if (scaleSeq != null)
                scaleSeq.Kill();

            //scale = 1.1f;
            frameColor = Color.red;
            scaleSeq = DOTween.Sequence();
            scaleSeq.Append(DOTween.To(() => scale, x => scale = x, 1.05f, .1f).SetEase(Ease.Linear));
            scaleSeq.Append(DOTween.To(() => scale, x => scale = x, 1, .4f).SetEase(Ease.Linear));
            scaleSeq.Insert(0, DOTween.To(() => frameColor, x => frameColor = x, normalColor, .5f));
            scaleSeq.OnComplete(() => scaleSeq = null);

        }

        #region animation variables
        private Sequence alertSeq;
        //private Sequence dangerSeq;
        private Sequence scaleSeq;

        private Color normalColor = new Color32(0x78, 0xf7, 0xff, 0xff);

        private float alertAlpha
        {
            get => alertIcon.style.unityBackgroundImageTintColor.value.a;
            set => alertIcon.style.unityBackgroundImageTintColor = new Color(1, 1, 1, value);
        }

        private float scale
        {
            get => this.style.scale.value.value.x;
            set => this.style.scale = new Vector2(value, value);
        }

        //private float dangerAlpha
        //{
        //    get => dangerIcon.style.unityBackgroundImageTintColor.value.a;
        //    set => dangerIcon.style.unityBackgroundImageTintColor = new Color(1, 1, 1, value);
        //}

        private Color frameColor
        {
            get => topLeftFrame.style.borderTopColor.value;
            set
            {
                topLeftFrame.style.borderTopColor = topLeftFrame.style.borderLeftColor =
                topRightFrame.style.borderTopColor = topRightFrame.style.borderRightColor =
                botLeftFrame.style.borderLeftColor = botLeftFrame.style.borderBottomColor =
                botRightFrame.style.borderRightColor = botRightFrame.style.borderBottomColor
                    = value;
            }
        }
        #endregion
    }
}