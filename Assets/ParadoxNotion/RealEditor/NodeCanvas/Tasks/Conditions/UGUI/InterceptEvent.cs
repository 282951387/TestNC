using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.EventSystems;


namespace NodeCanvas.Tasks.Conditions
{

    [Category("UGUI")]
    [Description("Returns true when the selected event is triggered on the selected agent.\nYou can use this for both GUI and 3D objects.\nPlease make sure that Unity Event Systems are setup correctly")]
    public class InterceptEvent : ConditionTask<Transform>
    {

        public EventTriggerType eventType;

        protected override string info
        {
            get { return string.Format("{0} on {1}", eventType.ToString(), agentInfo); }
        }

        protected override void OnEnable()
        {
            switch (eventType)
            {
                case (EventTriggerType.PointerEnter): router.onPointerEnter += OnPointerEnter; break;
                case (EventTriggerType.PointerExit): router.onPointerExit += OnPointerExit; break;
                case (EventTriggerType.PointerDown): router.onPointerDown += OnPointerDown; break;
                case (EventTriggerType.PointerUp): router.onPointerUp += OnPointerUp; break;
                case (EventTriggerType.PointerClick): router.onPointerClick += OnPointerClick; break;
                case (EventTriggerType.Drag): router.onDrag += OnDrag; break;
                case (EventTriggerType.Drop): router.onDrop += OnDrop; break;
                case (EventTriggerType.Scroll): router.onScroll += OnScroll; break;
                case (EventTriggerType.UpdateSelected): router.onUpdateSelected += OnUpdateSelected; break;
                case (EventTriggerType.Select): router.onSelect += OnSelect; break;
                case (EventTriggerType.Deselect): router.onDeselect += OnDeselect; break;
                case (EventTriggerType.Move): router.onMove += OnMove; break;
                case (EventTriggerType.Submit): router.onSubmit += OnSubmit; break;
            }
        }

        protected override void OnDisable()
        {
            switch (eventType)
            {
                case (EventTriggerType.PointerEnter): router.onPointerEnter -= OnPointerEnter; break;
                case (EventTriggerType.PointerExit): router.onPointerExit -= OnPointerExit; break;
                case (EventTriggerType.PointerDown): router.onPointerDown -= OnPointerDown; break;
                case (EventTriggerType.PointerUp): router.onPointerUp -= OnPointerUp; break;
                case (EventTriggerType.PointerClick): router.onPointerClick -= OnPointerClick; break;
                case (EventTriggerType.Drag): router.onDrag -= OnDrag; break;
                case (EventTriggerType.Drop): router.onDrop -= OnDrop; break;
                case (EventTriggerType.Scroll): router.onScroll -= OnScroll; break;
                case (EventTriggerType.UpdateSelected): router.onUpdateSelected -= OnUpdateSelected; break;
                case (EventTriggerType.Select): router.onSelect -= OnSelect; break;
                case (EventTriggerType.Deselect): router.onDeselect -= OnDeselect; break;
                case (EventTriggerType.Move): router.onMove -= OnMove; break;
                case (EventTriggerType.Submit): router.onSubmit -= OnSubmit; break;
            }
        }

        protected override bool OnCheck() { return false; }

        private void OnPointerEnter(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnPointerExit(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnPointerDown(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnPointerUp(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnPointerClick(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnDrag(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnDrop(ParadoxNotion.EventData<PointerEventData> eventData) { YieldReturn(true); }

        private void OnScroll(ParadoxNotion.EventData<PointerEventData> data) { YieldReturn(true); }

        private void OnUpdateSelected(ParadoxNotion.EventData<BaseEventData> eventData) { YieldReturn(true); }

        private void OnSelect(ParadoxNotion.EventData<BaseEventData> eventData) { YieldReturn(true); }

        private void OnDeselect(ParadoxNotion.EventData<BaseEventData> eventData) { YieldReturn(true); }

        private void OnMove(ParadoxNotion.EventData<AxisEventData> eventData) { YieldReturn(true); }

        private void OnSubmit(ParadoxNotion.EventData<BaseEventData> eventData) { YieldReturn(true); }
    }
}