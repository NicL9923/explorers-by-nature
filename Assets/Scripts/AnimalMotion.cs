using System.Collections.Generic;
using UnityEngine;

namespace ExplorersByNature
{
    /// <summary>Small procedural skeleton controller; locomotion remains owned by each scenery actor.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class AnimalMotion : MonoBehaviour
    {
        public string species;
        readonly Dictionary<string, Joint> joints = new Dictionary<string, Joint>();
        readonly List<Limb> limbs = new List<Limb>();
        Renderer[] renderers;
        Vector3 previous;
        float phase, clock, speed, blend, seed;
        sealed class Joint { public Transform bone; public Quaternion rest; public Vector3 axisX, axisY, axisZ; }
        sealed class Limb
        {
            public Transform upper, lower, foot;
            public Vector3 restFoot, restKnee;
            public Quaternion footRotation;
            public float upperLength, lowerLength, offset;
            public bool front;
        }
        public static void Attach(GameObject model, string path)
        {
            string name = path.Substring(path.LastIndexOf('/') + 1);
            if (name != "Deer" && name != "Rabbit" && name != "Fox" && name != "Beaver" && name != "Duck" && name != "Clover" && name != "Hen") return;
            model.AddComponent<AnimalMotion>().species = name;
        }
        void Start()
        {
            previous = transform.position;
            seed = Mathf.Repeat(previous.x * .731f + previous.z * .379f, 21);
            clock = seed;
            renderers = GetComponentsInChildren<Renderer>();
            foreach (Transform bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name == "Neck" || bone.name == "Head" || bone.name == "Tail" || bone.name.StartsWith("Ear"))
                    joints[bone.name] = new Joint { bone = bone, rest = bone.localRotation,
                        axisX = bone.InverseTransformDirection(transform.right), axisY = bone.InverseTransformDirection(transform.up), axisZ = bone.InverseTransformDirection(transform.forward) };
            }
            foreach (string pair in new[] { "FrontL", "FrontR", "HindL", "HindR" })
            {
                Transform upper = Find(pair + "Upper"), lower = Find(pair + "Lower"), foot = Find(pair + "Foot");
                if (upper == null || lower == null || foot == null) continue;
                limbs.Add(new Limb { upper = upper, lower = lower, foot = foot,
                    restFoot = transform.InverseTransformPoint(foot.position), restKnee = transform.InverseTransformPoint(lower.position),
                    footRotation = Quaternion.Inverse(transform.rotation) * foot.rotation,
                    upperLength = Vector3.Distance(upper.position, lower.position), lowerLength = Vector3.Distance(lower.position, foot.position),
                    front = pair.StartsWith("Front"), offset = pair == "FrontL" ? 0 : pair == "HindR" ? .25f : pair == "FrontR" ? .5f : .75f });
            }
            // Account for the full head-down and stepping poses in the imported bounds.
            foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>())
            { Bounds bounds = skin.localBounds; bounds.Expand(bounds.size * .7f); skin.localBounds = bounds; skin.updateWhenOffscreen = false; }
        }
        Transform Find(string name)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>()) if (child.name == name) return child;
            return null;
        }
        void Turn(string name, float pitch, float yaw = 0, float roll = 0)
        {
            if (!joints.TryGetValue(name, out Joint joint)) return;
            joint.bone.localRotation = joint.rest * Quaternion.AngleAxis(yaw, joint.axisY) * Quaternion.AngleAxis(pitch, joint.axisX) * Quaternion.AngleAxis(roll, joint.axisZ);
        }
        public static float IdleEnvelope(float time, float period, float start, float duration)
        {
            float t = Mathf.Repeat(time, period) - start;
            if (t <= 0 || t >= duration) return 0;
            return Mathf.SmoothStep(0, 1, Mathf.Min(t, duration - t) / Mathf.Min(1.2f, duration * .25f));
        }
        // Stance occupies 64% of the cycle: the foot retreats at constant speed as
        // the actor advances, then clears the ground during the shorter swing.
        public static Vector2 Step(float cycle, float stride, float lift)
        {
            float t = Mathf.Repeat(cycle, 1);
            if (t < .64f) return new Vector2(stride * (.5f - t / .64f), 0);
            float swing = (t - .64f) / .36f;
            return new Vector2(Mathf.Lerp(-stride * .5f, stride * .5f, Mathf.SmoothStep(0, 1, swing)), Mathf.Sin(swing * Mathf.PI) * lift);
        }
        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            Vector3 displacement = transform.position - previous; previous = transform.position;
            displacement.y = 0;
            float measured = displacement.magnitude / dt;
            // Spawns/network repositioning are not locomotion.
            speed = Mathf.Lerp(speed, measured > 4 ? 0 : measured, 1 - Mathf.Exp(-dt * 10));
            blend = Mathf.MoveTowards(blend, speed > .015f ? 1 : 0, dt * 5);
            clock += dt;
            float stride = species == "Deer" ? .48f : species == "Fox" ? .20f : species == "Rabbit" ? .13f : .10f;
            phase += speed * dt * .64f / stride;
            bool visible = false;
            foreach (Renderer renderer in renderers) if (renderer.isVisible) { visible = true; break; }
            if (!visible) return;
            float idle = 1 - blend;
            float graze = IdleEnvelope(clock, species == "Hen" ? 9 : 19, 3, species == "Hen" ? 3 : 9) * idle;
            float headPitch = Mathf.Sin(clock * 1.1f) * 2;
            float neckPitch = 0;
            if (species == "Deer" || species == "Clover")
            { neckPitch = graze * (species == "Deer" ? 94 : 68); headPitch += graze * (18 + Mathf.Sin(clock * 4) * 2); }
            else if (species == "Hen") { neckPitch = graze * 60; headPitch += graze * (25 + Mathf.Sin(clock * 9) * 7); }
            else if (species == "Rabbit") { neckPitch = graze * 14; headPitch += graze * Mathf.Sin(clock * 10) * 3; }
            else if (species == "Beaver") { neckPitch = graze * 12; headPitch += graze * Mathf.Sin(clock * 5) * 6; }
            else if (species == "Duck") headPitch += IdleEnvelope(clock, 16, 8, 4) * 22;
            Turn("Neck", neckPitch);
            Turn("Head", headPitch, Mathf.Sin(clock * .47f) * (graze > .5f ? 2 : 8));
            float twitch = IdleEnvelope(clock + seed, 7, 2, .6f) * Mathf.Sin(clock * 25) * 12;
            Turn("EarL", Mathf.Sin(clock * .9f) * 4, twitch, Mathf.Sin(clock * .63f) * 6);
            Turn("EarR", Mathf.Sin(clock * .81f + 2) * 4, -twitch * .6f, Mathf.Sin(clock * .73f + 1) * 6);
            Turn("Tail", species == "Beaver" ? Mathf.Sin(clock) * 2 : Mathf.Sin(clock * 1.4f) * 3,
                Mathf.Sin(clock * (species == "Clover" ? 2.8f : 1.7f)) * (species == "Fox" ? 9 : 6));
            foreach (Limb limb in limbs)
            {
                float offset = species == "Rabbit" ? (limb.front ? .1f : .6f) : limb.offset;
                Vector2 step = Step(phase + offset, stride, species == "Deer" ? .10f : .035f) * blend;
                Vector3 target = limb.restFoot + new Vector3(0, step.y, step.x);
                if (species == "Duck")
                { float paddle = clock * 5 + (limb.offset > 0 ? Mathf.PI : 0); target += new Vector3(0, Mathf.Sin(paddle) * .015f, Mathf.Cos(paddle) * .05f); }
                if (species == "Beaver" && limb.front)
                { target += new Vector3(0, graze * (.11f + Mathf.Sin(clock * 6 + limb.offset * 5) * .025f), graze * .025f); }
                Solve(limb, transform.TransformPoint(target));
            }
        }
        void Solve(Limb limb, Vector3 target)
        {
            Vector3 hip = limb.upper.position, delta = target - hip;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(limb.upperLength - limb.lowerLength) + .0001f, (limb.upperLength + limb.lowerLength) * .9999f);
            Vector3 direction = delta.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(transform.TransformPoint(limb.restKnee) - hip, direction);
            if (bend.sqrMagnitude < .000001f) bend = Vector3.ProjectOnPlane(limb.front ? transform.forward : -transform.forward, direction);
            bend.Normalize();
            float along = (limb.upperLength * limb.upperLength - limb.lowerLength * limb.lowerLength + distance * distance) / (2 * distance);
            Vector3 knee = hip + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, limb.upperLength * limb.upperLength - along * along));
            limb.upper.rotation = Quaternion.FromToRotation(limb.lower.position - hip, knee - hip) * limb.upper.rotation;
            limb.lower.rotation = Quaternion.FromToRotation(limb.foot.position - limb.lower.position, hip + direction * distance - limb.lower.position) * limb.lower.rotation;
            limb.foot.rotation = transform.rotation * limb.footRotation;
        }
    }
}
