using Drilling.Common.Managers;
using Drilling.Common.Interface;
using Drilling.Common.Motion;
using Drilling.Common.Alarm;
using Drilling.Common.InterLock;
using Drilling.Common.Station;

namespace Drilling.Common.Motion;

[CMotionControllerType("AUTOMATION1", "AUTOMATION_ONE", "A1")]
internal sealed class CAutomation1Motion(IInterfaceManager? interfaceManager, int deviceNo = 0)
    : CMotionController("AUTOMATION1", interfaceManager, deviceNo)
{
    protected override EN_EQP_MODULE PrimaryModule => EN_EQP_MODULE.Automation1;

    protected override string CommandPrefix => "AUTOMATION1";
}

