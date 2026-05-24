#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupPlayerAnimator
{
    const string ControllerPath = "Assets/Prefabs/PlayerAnimController.controller";
    const string AnimBasePath = "Assets/3D Models/Animaciones/Locomotion/";

    const string ParamSpeed = "Speed";
    const string ParamIsGrounded = "IsGrounded";
    const string ParamIsRecording = "IsRecording";

    [MenuItem("Echoes of You/FIX - Setup Player Animator", false, 82)]
    [MenuItem("Echoes/Setup Player Animator")]
    public static void Setup()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        ClearParameters(controller);
        controller.AddParameter(ParamSpeed, AnimatorControllerParameterType.Float);
        controller.AddParameter(ParamIsGrounded, AnimatorControllerParameterType.Bool);
        controller.AddParameter(ParamIsRecording, AnimatorControllerParameterType.Bool);

        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;
        ClearStateMachine(stateMachine);

        AnimationClip idleClip = LoadClip("idle.fbx");
        AnimationClip walkClip = LoadClip("walking.fbx");
        AnimationClip runClip = LoadClip("running.fbx");
        AnimationClip jumpClip = LoadClip("jump.fbx");
        AnimationClip fallClip = jumpClip != null ? jumpClip : idleClip;
        AnimationClip recordClip = idleClip;

        AnimatorState idle = stateMachine.AddState("Idle", new Vector3(0f, 0f, 0f));
        idle.motion = idleClip;

        AnimatorState walkRun = stateMachine.AddState("WalkRun", new Vector3(260f, 0f, 0f));
        walkRun.motion = CreateLocomotionTree(controller, idleClip, walkClip, runClip);

        AnimatorState jump = stateMachine.AddState("Jump", new Vector3(120f, -130f, 0f));
        jump.motion = jumpClip;

        AnimatorState fall = stateMachine.AddState("Fall", new Vector3(310f, -130f, 0f));
        fall.motion = fallClip;

        AnimatorState record = stateMachine.AddState("Record", new Vector3(120f, 130f, 0f));
        record.motion = recordClip;

        stateMachine.defaultState = idle;

        AddTransition(idle, walkRun, false, 0.12f, t =>
        {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);
        });

        AddTransition(walkRun, idle, false, 0.12f, t =>
        {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);
        });

        AnimatorStateTransition anyToJump = stateMachine.AddAnyStateTransition(jump);
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.06f;
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsGrounded);
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);

        AddTransition(jump, fall, true, 0.08f, t =>
        {
            t.exitTime = 0.72f;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsGrounded);
        });

        AddTransition(jump, idle, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);
        });

        AddTransition(jump, walkRun, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);
        });

        AddTransition(fall, idle, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);
        });

        AddTransition(fall, walkRun, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);
        });

        AnimatorStateTransition anyToRecord = stateMachine.AddAnyStateTransition(record);
        anyToRecord.hasExitTime = false;
        anyToRecord.duration = 0.08f;
        anyToRecord.canTransitionToSelf = false;
        anyToRecord.AddCondition(AnimatorConditionMode.If, 0f, ParamIsRecording);

        AddTransition(record, jump, false, 0.08f, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsGrounded);
        });

        AddTransition(record, idle, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, ParamSpeed);
        });

        AddTransition(record, walkRun, false, 0.1f, t =>
        {
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsRecording);
            t.AddCondition(AnimatorConditionMode.If, 0f, ParamIsGrounded);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, ParamSpeed);
        });

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Echoes] Player Animator configurado.");
    }

    static BlendTree CreateLocomotionTree(AnimatorController controller, AnimationClip idleClip, AnimationClip walkClip, AnimationClip runClip)
    {
        BlendTree blendTree = new BlendTree
        {
            name = "WalkRunBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = ParamSpeed
        };

        blendTree.AddChild(idleClip, 0f);
        blendTree.AddChild(walkClip != null ? walkClip : idleClip, 0.1f);
        blendTree.AddChild(runClip != null ? runClip : walkClip, 5.5f);
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        return blendTree;
    }

    static void AddTransition(AnimatorState from, AnimatorState to, bool hasExitTime, float duration, System.Action<AnimatorStateTransition> configure)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        configure?.Invoke(transition);
    }

    static AnimationClip LoadClip(string filename)
    {
        string path = AnimBasePath + filename;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }

        Debug.LogWarning("[Echoes] Clip no encontrado: " + path);
        return null;
    }

    static void ClearParameters(AnimatorController controller)
    {
        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);
    }

    static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = states.Length - 1; i >= 0; i--)
            stateMachine.RemoveState(states[i].state);

        ChildAnimatorStateMachine[] subStateMachines = stateMachine.stateMachines;
        for (int i = subStateMachines.Length - 1; i >= 0; i--)
            stateMachine.RemoveStateMachine(subStateMachines[i].stateMachine);

        AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;
        for (int i = anyStateTransitions.Length - 1; i >= 0; i--)
            stateMachine.RemoveAnyStateTransition(anyStateTransitions[i]);
    }
}
#endif
