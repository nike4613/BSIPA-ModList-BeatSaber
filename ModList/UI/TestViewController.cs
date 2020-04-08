using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.ModList.BeatSaber.UI
{
    [HotReload(PathMap = new[] { "C:\\", CompileConstants.SolutionDirectory })]
    internal class TestViewController : BSMLAutomaticViewController
    {
        [UIValue("values")]
        public List<object> Values { get; } = new List<object>()
        {
            new TestListObject(1),
            new TestListObject(2),
            new TestListObject(3),
            new TestListObject(4),
            new TestListObject(5)
        };

        [UIValue("cells")]
        public List<CustomListTableData.CustomCellInfo> Cells { get; } = new List<CustomListTableData.CustomCellInfo>
        {
            new CustomListTableData.CustomCellInfo("haha 1", "sub1"),
            new CustomListTableData.CustomCellInfo("haha 2", "sub2"),
            new CustomListTableData.CustomCellInfo("haha 3", "sub3"),
            new CustomListTableData.CustomCellInfo("haha 4", "sub4"),
        };

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

            Logger.log.Debug("Test VC activated");
        }

        private class TestListObject
        {
            [UIValue("sval1")]
            public string StringValue1 { get; } = "string value 1";
            [UIValue("sval2")]
            public string StringValue2 { get; } = "string value 2";
            [UIValue("sval3")]
            public string StringValue3 { get; } = "string value 3";
            [UIValue("sval4")]
            public string StringValue4 { get; } = "string value 4";
            [UIValue("sval5")]
            public string StringValue5 { get; } = "string value 5";
            [UIValue("ival")]
            public int IntValue { get; }
            public TestListObject(int value)
                => IntValue = value;
        }
    }
}
