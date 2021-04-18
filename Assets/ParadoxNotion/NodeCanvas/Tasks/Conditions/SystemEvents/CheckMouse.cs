using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;


namespace NodeCanvas.Tasks.Conditions
{

    [Category("System Events")]
    public class CheckMouse : ConditionTask<Collider>
    {

        public MouseInteractionTypes checkType = MouseInteractionTypes.MouseEnter;

        protected override string info
        {
            get { return checkType.ToString(); }
        }

        protected override bool OnCheck() { return false; }

        protected override void OnEnable()
        {
            router.onMouseEnter += OnMouseEnter;
            router.onMouseExit += OnMouseExit;
            router.onMouseOver += OnMouseOver;
        }

        protected override void OnDisable()
        {
            router.onMouseEnter -= OnMouseEnter;
            router.onMouseExit -= OnMouseExit;
            router.onMouseOver -= OnMouseOver;
        }

        private void OnMouseEnter(ParadoxNotion.EventData msg)
        {
            if (checkType == MouseInteractionTypes.MouseEnter)
            {
                YieldReturn(true);
            }
        }

        private void OnMouseExit(ParadoxNotion.EventData msg)
        {
            if (checkType == MouseInteractionTypes.MouseExit)
            {
                YieldReturn(true);
            }
        }

        private void OnMouseOver(ParadoxNotion.EventData msg)
        {
            if (checkType == MouseInteractionTypes.MouseOver)
            {
                YieldReturn(true);
            }
        }
    }
}