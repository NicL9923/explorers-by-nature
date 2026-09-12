using UnityEngine;

namespace ExplorersByNature
{
    public sealed class WildlifeRoamer : MonoBehaviour
    {
        public Transform[] legs;
        public Transform observer;
        Vector3 home;
        Vector3 target;
        float phase;
        float wait;
        System.Random random;

        void Start()
        {
            home = transform.position;
            random = new System.Random(Mathf.RoundToInt(home.x * 100 + home.z));
            PickTarget();
        }

        void PickTarget()
        {
            // Keep the first wildlife on its meadow side of the river.
            float x = Mathf.Min(-8, home.x + (float)(random.NextDouble() - .5) * 26);
            float z = home.z + (float)(random.NextDouble() - .5) * 26;
            target = ValleyShape.Ground(x, z);
        }

        void Update()
        {
            bool watching = observer != null && Vector3.Distance(observer.position, transform.position) < 8;
            if (watching || wait > 0)
            {
                wait -= Time.deltaTime;
                foreach (Transform leg in legs) leg.localRotation = Quaternion.identity;
                return;
            }
            Vector3 delta = target - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude < .8f) { wait = 3; PickTarget(); return; }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), Time.deltaTime * 2);
            Vector3 next = transform.position + transform.forward * (.8f * Time.deltaTime);
            transform.position = ValleyShape.Ground(next.x, next.z);
            phase += Time.deltaTime * 4;
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase + (i % 2) * Mathf.PI) * 18, 0, 0);
        }
    }
}
