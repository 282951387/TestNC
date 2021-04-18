using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.UI;

namespace NodeCanvas.Tasks.Conditions
{

    [Category("UGUI")]
    public class ButtonClicked : ConditionTask
    {

        [RequiredField]
        public BBParameter<UnityEngine.UI.Button> button;

        protected override string info
        {
            get { return string.Format("Button {0} Clicked", button.ToString()); }
        }

        protected override string OnInit()
        {
            button.value.onClick.AddListener(OnClick);
            return null;
        }

        protected override bool OnCheck() { return false; }

        private void OnClick() { YieldReturn(true); }
    }
}