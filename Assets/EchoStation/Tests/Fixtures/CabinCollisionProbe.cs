using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EchoStation.Tests.Fixtures
{
    public static class CabinCollisionProbe
    {
        [Serializable]public class Check{public string name,hit;public bool expectedBlocked,blocked,passed;public float hitDistance;}
        [Serializable]public class Result{public bool passed;public Check[] checks;}
        public static Result Run(PhysicsScene physics)
        {
            if(!physics.IsValid())throw new ArgumentException("A valid PhysicsScene is required.");
            if(physics.Equals(Physics.defaultPhysicsScene))Physics.SyncTransforms();else physics.Simulate(.02f);
            var checks=new List<Check>();
            var route=new[]{new Vector3(0,.05f,-6),new Vector3(0,.05f,-3),new Vector3(2,.05f,-1),new Vector3(3.8f,.05f,1),new Vector3(6,.05f,1),new Vector3(8,.05f,2.2f)};
            for(var i=0;i<route.Length-1;i++)
            {
                checks.Add(Sweep(physics,"RouteForward_"+i,route[i],route[i+1]-route[i],false));
                checks.Add(Sweep(physics,"RouteReverse_"+i,route[i+1],route[i]-route[i+1],false));
            }
            checks.Add(Sweep(physics,"MainWestBoundary",new Vector3(0,.05f,-2),Vector3.left*7,true,"Main_WallWest"));
            checks.Add(Sweep(physics,"MainNorthBoundary",new Vector3(0,.05f,1),Vector3.forward*5,true,"Main_WallNorth"));
            checks.Add(Sweep(physics,"MainSouthSolid",new Vector3(2,.05f,-2),Vector3.back*5,true,"Main_WallSouthRight"));
            checks.Add(Sweep(physics,"MainEastSolid",new Vector3(0,.05f,-3),Vector3.right*7,true,"Main_WallEastLower"));
            checks.Add(Sweep(physics,"StorageEastBoundary",new Vector3(7,.05f,1),Vector3.right*4,true,"Storage_WallEast"));
            checks.Add(Sweep(physics,"FoyerSouthBoundary",new Vector3(0,.05f,-6),Vector3.back*3,true,"Foyer_WallSouth"));
            checks.Add(Sweep(physics,"Ceiling",new Vector3(0,.05f,-2),Vector3.up*5,true,"Main_Ceiling"));
            checks.Add(Sweep(physics,"Floor",new Vector3(0,.5f,-2),Vector3.down*2,true,"Main_Floor"));
            checks.Add(Sweep(physics,"EquipmentObstacle",new Vector3(-1.8f,.05f,0),Vector3.left*3,true,"Main_EquipmentCabinet"));
            checks.Add(Sweep(physics,"CrateObstacle",new Vector3(7,.05f,1.5f),Vector3.back*4,true,"Storage_Crate"));
            checks.Add(Sweep(physics,"ShelfObstacle",new Vector3(7,.05f,1.5f),Vector3.forward*4,true,"Storage_ShelfProxy"));
            checks.Add(Sweep(physics,"MainDoorSide",new Vector3(1.1f,.05f,-6),Vector3.forward*4,true,"Main_WallSouthRight"));
            checks.Add(Sweep(physics,"StorageDoorSide",new Vector3(4,.05f,2.2f),Vector3.right*3,true,"Main_WallEastUpper"));
            return new Result{passed=checks.All(c=>c.passed),checks=checks.ToArray()};
        }
        private static Check Sweep(PhysicsScene physics,string name,Vector3 feet,Vector3 delta,bool expected,string expectedName=null)
        {
            var bottom=feet+Vector3.up*.3f;var top=feet+Vector3.up*(1.75f-.3f);
            var blocked=physics.CapsuleCast(bottom,top,.3f,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            return new Check{name=name,expectedBlocked=expected,blocked=blocked,hit=blocked?hit.collider.name:"",hitDistance=hit.distance,
                passed=blocked==expected&&(!expected||expectedName==null||hit.collider.name==expectedName)};
        }
    }
}
