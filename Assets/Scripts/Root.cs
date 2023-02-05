using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Root : MonoBehaviour
{
    public GameObject rootBase;
    public GameObject head;
    public Transform target;
    public AIPath headPath;
    public int segmentCount;
    public float rootLength;
    public float maxRootLength;
    public float segmentLength;
    public float rootSpeed;
    //public float growSpeed;
    public float detectionRadius;

    public bool pointingUp;

    public HeadState headState;

    public enum HeadState
    {
        Idle,
        Chasing,
        Retreating
    }
    
    class Segment
    {
        public Vector2 start;
        public Vector2 end;

        public float angle;
        public float length;

        public Segment(float x, float y, float length, float angle)
        {
            this.angle = angle;
            this.length = length;
            this.start = new Vector2(x, y);
            float dx = length * Mathf.Cos(angle);
            float dy = length * Mathf.Sin(angle);
            this.end = this.start + new Vector2(dx, dy);
        }

        public void Update()
        {
            float dx = length * Mathf.Cos(Mathf.Deg2Rad * angle);
            float dy = length * Mathf.Sin(Mathf.Deg2Rad * angle);
            Vector2 oldEnd = this.end;
            Vector2 newEnd = this.start + new Vector2(dx, dy);

            this.end = newEnd;
            
            Vector2 deltaDirection = newEnd - oldEnd;

            RaycastHit2D hitInfo = Physics2D.Raycast(oldEnd + deltaDirection.normalized * 0.01f, deltaDirection.normalized, deltaDirection.magnitude, LayerMask.GetMask(new[] { "RootHelper" }));
            if(hitInfo.collider != null)
            {
                //this.end = oldEnd + (Vector2)Vector3.Cross(Vector3.forward, deltaDirection.normalized) * deltaDirection.magnitude;
                this.end += hitInfo.normal * (deltaDirection.magnitude - hitInfo.distance);
                //this.end += hitInfo.normal * (deltaDirection.magnitude - hitInfo.distance) * Time.deltaTime;
            }

            RaycastHit2D hitInfo_end = Physics2D.Raycast(this.end, this.start - this.end, (this.start - this.end).magnitude, LayerMask.GetMask(new[] { "RootHelper" }));
            RaycastHit2D hitInfo_start = Physics2D.Raycast(this.start, this.end - this.start, this.length, LayerMask.GetMask(new[] { "RootHelper" }));
            if(hitInfo_end.collider || hitInfo_start.collider)
            {
                //Vector2 startCorrection = Vector2.zero;
                //Vector2 endCorrection = Vector2.zero;

                //startCorrection.x += hitInfo_start.normal.x;//  > 0.0f ? 1.0f - hitInfo_start.normal.x : -1.0f - hitInfo_start.normal.x;
                //startCorrection.y += hitInfo_start.normal.y;//  > 0.0f ? 1.0f - hitInfo_start.normal.y : -1.0f - hitInfo_start.normal.y;
                //endCorrection.x += hitInfo_end.normal.x;//  > 0.0f ? 1.0f - hitInfo_end.normal.x : -1.0f - hitInfo_end.normal.x;
                //endCorrection.y += hitInfo_end.normal.y;//  > 0.0f ? 1.0f - hitInfo_end.normal.y : -1.0f - hitInfo_end.normal.y;

                this.start += hitInfo_end.normal * (this.start - this.end).magnitude;
                //this.start += hitInfo_end.normal * (this.start - this.end).magnitude * Time.deltaTime;
                //this.start += startCorrection * hitInfo_start.distance;
                //this.end += endCorrection * hitInfo_end.distance;
            }

        }

        float GetAngleRad(Vector2 from, Vector2 to)
        {
            return Mathf.Atan2(to.y - from.y, to.x - from.x);
        }

        public void PointAt(Vector2 target, float rootSpeed)
        {
            Vector2 dir = target - this.start;

            float newAngle = Mathf.Rad2Deg * GetAngleRad(this.start, target);

            angle = Mathf.LerpAngle(angle, newAngle, rootSpeed * Time.deltaTime * 2.0f);
            dir = dir.normalized * this.length * -1.0f;

            Vector2 oldStart = this.start;
            Vector2 newStart = target + dir;

            this.start = newStart;

            Vector2 deltaDirection = newStart - oldStart;

            RaycastHit2D hitInfo = Physics2D.Raycast(oldStart + deltaDirection.normalized * 0.01f, deltaDirection.normalized, deltaDirection.magnitude, LayerMask.GetMask(new[] { "RootHelper" }));
            if (hitInfo.collider != null)
            {
                //this.end = oldEnd + (Vector2)Vector3.Cross(Vector3.forward, deltaDirection.normalized) * deltaDirection.magnitude;
                this.start += hitInfo.normal * (deltaDirection.magnitude - hitInfo.distance);
            }
        }
    }

    List<Segment> segments = new List<Segment>();

    // Start is called before the first frame update
    void Start()
    {
        this.headPath = this.head.GetComponent<AIPath>();

        segments.Add(new Segment(rootBase.transform.position.x, rootBase.transform.position.y, segmentLength, 90.0f));

        for (int i = 1; i < segmentCount; i++)
        {
            segments.Add(new Segment(segments[i - 1].end.x, segments[i - 1].end.y, segmentLength, segments[i - 1].angle));
        }

        this.rootLength = 0;
        foreach(Segment segment in segments)
        {
            this.rootLength += segment.length;
        }
    }


    // Update is called once per frame
    void Update()
    {
        Segment currentLastSeg = segments[segments.Count - 1];
        switch (headState)
        {
            case HeadState.Idle:
                if((target.position - head.transform.position).magnitude < detectionRadius)
                    this.headState = HeadState.Chasing;
                float outDirY = pointingUp ? 1.0f : -1.0f;
                headPath.destination = Vector2.Lerp(head.transform.position, new Vector2(rootBase.transform.position.x + Mathf.Sin(Time.time) * 0.2f, rootBase.transform.position.y + rootLength * outDirY * 0.8f + (Mathf.Abs(Mathf.Cos(Time.time)) + outDirY) * 0.3f), 0.5f);
                break;
            case HeadState.Chasing:
                if ((target.position - head.transform.position).magnitude > detectionRadius)
                    this.headState = HeadState.Idle;
                headPath.destination = target.position;
                break;
            case HeadState.Retreating:
                break;
        }

        int ITERATION_COUNT = 1;
        for (int _ = 0; _ < ITERATION_COUNT; _++)
        {
            segments[segments.Count - 1].PointAt(head.transform.position, rootSpeed);

            for (int i = segments.Count - 2; i >= 0; i--)
            {
                segments[i].PointAt(segments[i + 1].start, rootSpeed);
            }

            segments[0].start = rootBase.transform.position;
            for (int i = 1; i < segments.Count; i++)
            {
                segments[i].start = segments[i - 1].end;
            }
            //currentLastSeg.end = head.transform.position;

            foreach (Segment seg in segments)
                seg.Update();
        }

        head.transform.position = Vector3.Lerp(head.transform.position, (Vector3)currentLastSeg.end, 0.3f);
        Draw();
    }
    
    void Draw()
    {
        LineRenderer lr = this.GetComponent<LineRenderer>();

        List<Vector3> positions = new List<Vector3>();
        positions.Add(new Vector3(segments[0].start.x, segments[0].start.y));
        foreach(Segment seg in segments)
        {
            //positions.Add(new Vector3(seg.start.x, seg.start.y));
            positions.Add(new Vector3(seg.end.x, seg.end.y));
        }

        lr.positionCount = positions.Count;
        lr.SetPositions(positions.ToArray());
    }
}
