using UnityEngine;

namespace ExplorersByNature
{
    /// <summary>World-space Verlet guide with pinned roots, inextensible segments and a skin half-space.</summary>
    public sealed class HairGuide
    {
        public const int Segments = 6;
        public readonly Vector3[] points = new Vector3[Segments + 1];
        readonly Vector3[] previous = new Vector3[Segments + 1];
        Vector3 lastRoot;
        float previousStep;
        bool initialized;

        public void Reset(Vector3 root, Vector3 growth)
        {
            for (int i = 0; i <= Segments; i++) points[i] = previous[i] = root + growth * (i / (float)Segments);
            lastRoot = root; previousStep = 0; initialized = true;
        }

        public void Step(Vector3 root, Vector3 growth, Vector3 normal, Vector3 wind, float dt)
        {
            if (!initialized || dt > .2f || (root - lastRoot).sqrMagnitude > .25f)
            { Reset(root, growth); return; }
            if (dt <= 0) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt * 120));
            float h = dt / steps;
            Vector3 oldRoot = lastRoot;
            float segment = growth.magnitude / Segments;
            for (int step = 0; step < steps; step++)
            {
                Vector3 anchor = Vector3.Lerp(oldRoot, root, (step + 1f) / steps);
                points[0] = previous[0] = anchor;
                for (int i = 1; i <= Segments; i++)
                {
                    Vector3 current = points[i];
                    Vector3 velocity = (current - previous[i]) * (previousStep > 0 ? h / previousStep : 1);
                    float t = i / (float)Segments;
                    Vector3 rest = anchor + growth * t;
                    // Strong root shape memory yields to the freely moving ends.
                    Vector3 force = (rest - current) * Mathf.Lerp(380, 70, t) + wind * (2.2f * t) + Vector3.down * (.35f * t);
                    points[i] += velocity * Mathf.Exp(-9 * h) + force * (h * h);
                    previous[i] = current;
                }
                previousStep = h;
                // Forward projection gives every edge exact length, without moving the pinned root.
                for (int iteration = 0; iteration < 3; iteration++)
                    for (int i = 1; i <= Segments; i++)
                    {
                        Vector3 delta = points[i] - points[i - 1];
                        points[i] = points[i - 1] + (delta.sqrMagnitude > 1e-12f ? delta.normalized : growth.normalized) * segment;
                        float penetration = Vector3.Dot(points[i] - anchor, normal);
                        if (penetration < 0)
                        {
                            Vector3 tangent = Vector3.ProjectOnPlane(points[i] - points[i - 1], normal);
                            // Reflect inward motion. The segment length remains exact and the tip stays outside skin.
                            Vector3 reflected = points[i] - points[i - 1] - normal * (2 * Mathf.Min(0, Vector3.Dot(points[i] - points[i - 1], normal)));
                            points[i] = points[i - 1] + (reflected.sqrMagnitude > 1e-12f ? reflected.normalized : tangent.normalized) * segment;
                        }
                    }
            }
            lastRoot = root;
        }
    }
}
