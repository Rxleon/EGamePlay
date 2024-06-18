using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using EGamePlay.Combat;
using NaughtyBezierCurves;
using System.Linq;
using System.Reflection;

#if EGAMEPLAY_ET
using Unity.Mathematics;
using Vector3 = Unity.Mathematics.float3;
using Quaternion = Unity.Mathematics.quaternion;
using JsonIgnore = MongoDB.Bson.Serialization.Attributes.BsonIgnoreAttribute;
#endif

namespace EGamePlay
{
    public class ExecuteClipData
#if UNITY
         : SerializedScriptableObject
#endif
    {
        public double TotalTime { get; set; }
        public double StartTime;
        public double EndTime;

        [ShowInInspector]
        [DelayedProperty]
        [PropertyOrder(-1)]
        public string Name
        {
            get { return name; }
            set { name = value; /*AssetDatabase.ForceReserializeAssets(new string[] { AssetDatabase.GetAssetPath(this) }); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();*/ }
        }

#if NOT_UNITY
        private string name;
#endif

        public ExecuteClipType ExecuteClipType;

        [Space(10)]
        [ShowIf("ExecuteClipType", ExecuteClipType.ActionEvent)]
        public ActionEventData ActionEventData;

        [Space(10)]
        [ShowIf("ExecuteClipType", ExecuteClipType.ItemExecute)]
        public CollisionExecuteData CollisionExecuteData;

#if UNITY
        [Space(10)]
        [ShowIf("ExecuteClipType", ExecuteClipType.Animation), JsonIgnore]
        public AnimationData AnimationData;

        [Space(10)]
        [ShowIf("ExecuteClipType", ExecuteClipType.Audio), JsonIgnore]
        public AudioData AudioData;

        [Space(10)]
        [ShowIf("ExecuteClipType", ExecuteClipType.ParticleEffect), JsonIgnore]
        public ParticleEffectData ParticleEffectData;
#endif

        [LabelText("效果列表"), Space(30)]
        [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = false, ShowItemCount = false, HideAddButton = true)]
        [HideReferenceObjectPicker]
        public List<ItemEffect> Effects = new List<ItemEffect>();

        [OnInspectorGUI("BeginBox", append: false)]
        [HorizontalGroup(PaddingLeft = 40, PaddingRight = 40)]
        [HideLabel, OnValueChanged("AddEffect"), ValueDropdown("EffectTypeSelect"), JsonIgnore]
        public string EffectTypeName = "(添加效果)";

        public IEnumerable<string> EffectTypeSelect()
        {
            var types = typeof(ItemEffect).Assembly.GetTypes()
                .Where(x => !x.IsAbstract)
                .Where(x => typeof(ItemEffect).IsAssignableFrom(x))
                .Where(x => x.GetCustomAttribute<EffectAttribute>() != null)
                .OrderBy(x => x.GetCustomAttribute<EffectAttribute>().Order)
                .Select(x => x.GetCustomAttribute<EffectAttribute>().EffectType);

            var results = types.ToList();
            results.Insert(0, "(添加效果)");
            return results;
        }

        private void AddEffect()
        {
            if (EffectTypeName != "(添加效果)")
            {
                var effectType = typeof(ItemEffect).Assembly.GetTypes()
                    .Where(x => !x.IsAbstract)
                    .Where(x => typeof(ItemEffect).IsAssignableFrom(x))
                    .Where(x => x.GetCustomAttribute<EffectAttribute>() != null)
                    .Where(x => x.GetCustomAttribute<EffectAttribute>().EffectType == EffectTypeName)
                    .FirstOrDefault();
                var effect = Activator.CreateInstance(effectType) as ItemEffect;
                effect.Enabled = true;
                Effects.Add(effect);
                EffectTypeName = "(添加效果)";
            }
        }

#if UNITY_EDITOR
        private void DrawSpace()
        {
            GUILayout.Space(20);
        }

        private void BeginBox()
        {
            GUILayout.Space(10);
        }

        private void EndBox()
        {
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
            GUILayout.Space(30);
            Sirenix.Utilities.Editor.SirenixEditorGUI.DrawThickHorizontalSeparator();
            GUILayout.Space(10);
        }
#endif

        public float Duration { get => (float)(EndTime - StartTime); }

        public ExecuteClipData GetClipTime()
        {
            return this;
        }
    }

    public enum ExecuteClipType
    {
        ItemExecute = 0,
        ActionEvent = 1,
        Animation = 2,
        Audio = 3,
        ParticleEffect = 4,
    }

    [LabelText("目标传入类型")]
    public enum ExecutionTargetInputType
    {
        [LabelText("None")]
        None = 0,
        [LabelText("传入目标实体")]
        Target = 1,
        [LabelText("传入目标点")]
        Point = 2,
    }

    [LabelText("事件类型")]
    public enum FireEventType
    {
        [LabelText("触发赋给效果")]
        AssignEffect = 0,
        [LabelText("触发新执行体")]
        TriggerNewExecution = 1,
        //[LabelText("触发防御效果")]
        //TriggerDefenseEffect = 2,
    }

    [LabelText("触发类型"), Flags]
    public enum FireType
    {
        None = 0,
        [LabelText("初始触发")]
        StartTrigger = 1 << 1,
        [LabelText("碰撞触发单次")]
        CollisionTrigger = 1 << 2,
        [LabelText("结束触发")]
        EndTrigger = 1 << 3,
        [LabelText("碰撞触发多次")]
        CollisionTriggerMultiple = 1 << 4,
    }

    [Serializable]
    public class ActionEventData
    {
        public FireType FireType;

        [HideIf("FireType", FireType.None)]
        public FireEventType ActionEventType;

        [JsonIgnore]
        public bool IsTriggerAssign
        {
            get
            {
                return FireType != FireType.None && ActionEventType == FireEventType.AssignEffect;
            }
        }

        [JsonIgnore]
        public bool IsTriggerExecution
        {
            get
            {
                return FireType != FireType.None && ActionEventType == FireEventType.TriggerNewExecution;
            }
        }

        [ShowIf("IsTriggerAssign")]
        public EffectApplyType EffectApply;

        [ShowIf("IsTriggerAssign")]
        public EffectApplyTarget EffectApplyTarget;

        [ShowIf("IsTriggerExecution")]
        [LabelText("新执行体")]
        public string NewExecution;
    }

    [LabelText("碰撞体执行类型")]
    public enum CollisionExecuteType
    {
        [LabelText("脱手执行")]
        OutOfHand = 0,
        [LabelText("执手执行")]
        InHand = 1,
    }

    [Serializable]
    public class CollisionExecuteData
    {
        public CollisionExecuteType ExecuteType;
        public ActionEventData ActionData;

        [Space(10)]
        public CollisionShape Shape;
        [ShowIf("Shape", CollisionShape.Sphere), LabelText("半径")]
        public double Radius;

        [ShowIf("Shape", CollisionShape.Box)]
        public Vector3 Center;
        [ShowIf("Shape", CollisionShape.Box)]
        public Vector3 Size;

        [Space(10)]
        public CollisionMoveType MoveType;

        [ShowIf("MoveType", CollisionMoveType.FixedPosition)]
        public Vector3 FixedPoint;

        [Space(10)]
        [DelayedProperty, JsonIgnore]
        public GameObject ObjAsset;

        [ShowIf("ShowSpeed")]
        public double Speed = 1;
        public bool ShowSpeed { get => MoveType != CollisionMoveType.FixedPosition && MoveType != CollisionMoveType.SelectedPosition && MoveType != CollisionMoveType.SelectedDirection; }
        public bool ShowPoints { get => MoveType == CollisionMoveType.PathFly || MoveType == CollisionMoveType.SelectedDirectionPathFly; }
        [ShowIf("ShowPoints")]
        public BezierCurve3D BezierCurve;

        public List<BezierPoint3D> GetCtrlPoints()
        {
            var list = new List<BezierPoint3D>();
            if (BezierCurve != null)
            {
                foreach (var item in BezierCurve.KeyPoints)
                {
                    var newPoint = new BezierPoint3D();
                    newPoint.LocalPosition = item.LocalPosition;
                    newPoint.HandleStyle = item.HandleStyle;
                    newPoint.LeftHandleLocalPosition = item.LeftHandleLocalPosition;
                    newPoint.RightHandleLocalPosition = item.RightHandleLocalPosition;
                    list.Add(newPoint);
                }
            }
            return list;
        }
    }

    [Serializable]
    public class ParticleEffectData
    {
#if UNITY
        public GameObject ParticleEffect;
#endif
    }

    [Serializable]
    public class AnimationData
    {
#if UNITY
        public AnimationClip AnimationClip;
#endif
    }

    [Serializable]
    public class AudioData
    {
#if UNITY
        public AudioClip AudioClip;
#endif
    }
}
