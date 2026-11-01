using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoStation.Echo
{
    public interface IMovementPort
    {
        Vector3 Position{get;}NavigationState State{get;}
        bool TryMove(Vector3 destination);
        void Cancel();
    }
    public sealed class MovementLease
    {
        private readonly MovementAuthority authority;private readonly ulong generation;
        internal MovementLease(MovementAuthority owner,ulong value){authority=owner;generation=value;}
        public bool IsValid=>authority.IsCurrent(this,generation);
        public Vector3 Position=>authority.Port.Position;public NavigationState State=>authority.Port.State;
        public bool TryMove(Vector3 destination)=>IsValid&&authority.Port.TryMove(destination);
        public bool Stop(){if(!IsValid)return false;authority.Port.Cancel();return true;}
    }
    public sealed class MovementAuthority
    {
        internal IMovementPort Port{get;}
        private ulong generation;private MovementLease current;
        public MovementAuthority(IMovementPort port){Port=port??throw new ArgumentNullException(nameof(port));}
        internal bool IsCurrent(MovementLease lease,ulong value)=>ReferenceEquals(current,lease)&&generation==value;
        internal MovementLease Acquire(){if(current!=null)Revoke(current);generation++;return current=new MovementLease(this,generation);}
        internal void Revoke(MovementLease lease){if(!ReferenceEquals(current,lease)||current==null)return;generation++;current=null;Port.Cancel();}
    }
    public enum NodeStatus{Running,Success,Failure}
    public enum BehaviorTreeStatus{Idle,Running,Success,Failure,Cancelled}
    public sealed class BehaviorContext
    {
        public BehaviorIntent Intent{get;}public MovementLease Movement{get;}
        internal BehaviorContext(BehaviorIntent intent,MovementLease movement){Intent=intent;Movement=movement;}
    }
    public interface IBehaviorNode
    {
        void Enter(BehaviorContext context);
        NodeStatus Tick(float deltaSeconds);
        void Exit();
    }
    public sealed class IntentBehaviorTree:IIntentSink,IDisposable
    {
        private readonly Dictionary<IntentKind,Func<IBehaviorNode>> factories;
        private readonly MovementAuthority movement;private readonly Action<string,bool> speech;
        private IBehaviorNode active;private MovementLease lease;private bool callback,pendingCancel,disposed;
        public BehaviorTreeStatus Status{get;private set;}=BehaviorTreeStatus.Idle;
        public IntentKind? CurrentMode{get;private set;}
        public string LastError{get;private set;}
        public IntentBehaviorTree(IMovementPort port,IDictionary<IntentKind,Func<IBehaviorNode>> nodes,Action<string,bool> onSpeech=null)
        {
            movement=new MovementAuthority(port);factories=new Dictionary<IntentKind,Func<IBehaviorNode>>(nodes??throw new ArgumentNullException(nameof(nodes)));speech=onSpeech;
            foreach(var entry in factories)if(!IsBehavior(entry.Key)||entry.Value==null)throw new ArgumentException("Only four behavior factories are allowed.");
        }
        public bool TryBeginBehavior(BehaviorIntent intent)
        {
            if(disposed||callback||!IsBehavior(intent.Kind)||!factories.TryGetValue(intent.Kind,out var factory))return false;
            IBehaviorNode candidate;try{candidate=factory();}catch(Exception e){LastError=e.ToString();return false;}if(candidate==null)return false;
            LastError=null;if(!Finish(BehaviorTreeStatus.Cancelled))return false;
            lease=movement.Acquire();active=candidate;CurrentMode=intent.Kind;Status=BehaviorTreeStatus.Running;callback=true;
            try{candidate.Enter(new BehaviorContext(intent,lease));}
            catch(Exception e){LastError=e.ToString();}
            finally{callback=false;}
            if(LastError!=null){Finish(BehaviorTreeStatus.Failure);return false;}
            if(pendingCancel){pendingCancel=false;Finish(BehaviorTreeStatus.Cancelled);return false;}return true;
        }
        public void Tick(float deltaSeconds)
        {
            if(!float.IsFinite(deltaSeconds)||deltaSeconds<0)throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if(disposed||callback||active==null)return;NodeStatus result=NodeStatus.Failure;callback=true;
            try{result=active.Tick(deltaSeconds);}catch(Exception e){LastError=e.ToString();}finally{callback=false;}
            if(LastError!=null){Finish(BehaviorTreeStatus.Failure);return;}
            if(pendingCancel){pendingCancel=false;Finish(BehaviorTreeStatus.Cancelled);return;}
            if(result!=NodeStatus.Running)Finish(result==NodeStatus.Success?BehaviorTreeStatus.Success:BehaviorTreeStatus.Failure);
        }
        public void Cancel(){if(callback){pendingCancel=true;return;}Finish(BehaviorTreeStatus.Cancelled);}
        private bool Finish(BehaviorTreeStatus result)
        {
            if(active==null)return true;var previous=active;var previousLease=lease;active=null;lease=null;callback=true;var success=true;
            try{previous.Exit();}catch(Exception e){LastError=e.ToString();success=false;result=BehaviorTreeStatus.Failure;}
            finally{callback=false;movement.Revoke(previousLease);CurrentMode=null;Status=result;pendingCancel=false;}
            return success;
        }
        public void Speak(string content,bool refusal)=>speech?.Invoke(content,refusal);
        public void Dispose(){if(disposed)return;Cancel();disposed=true;}
        private static bool IsBehavior(IntentKind kind)=>kind==IntentKind.Follow||kind==IntentKind.Investigate||kind==IntentKind.Wait||kind==IntentKind.Warn;
    }
}
