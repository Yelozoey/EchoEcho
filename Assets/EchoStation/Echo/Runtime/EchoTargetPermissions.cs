using UnityEngine;

namespace EchoStation.Echo
{
    [DisallowMultipleComponent]
    public sealed class EchoTargetPermissions : MonoBehaviour
    {
        [SerializeField]private bool canInvestigate,isDanger;
        [SerializeField]private Transform approachPoint;
        [SerializeField,TextArea]private string scanFact;
        public bool CanInvestigate=>canInvestigate;public bool IsDanger=>isDanger;
        public Transform ApproachPoint=>approachPoint;
        public string ScanFact=>scanFact;
        public void Configure(bool informationInvestigation,bool dangerWarning){canInvestigate=informationInvestigation;isDanger=dangerWarning;}
        public void SetApproachPoint(Transform approvedPoint)=>approachPoint=approvedPoint;
        public void SetScanFact(string summary)
        {if(string.IsNullOrWhiteSpace(summary)||summary.Length>120)throw new System.ArgumentException("Scan fact must be a short summary.");scanFact=summary;}
    }
}
