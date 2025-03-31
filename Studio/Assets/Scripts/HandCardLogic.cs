using UnityEngine;

public class HandCardLogic : MonoBehaviour
{
    public float fanAngle = 45f;    // �����������νǶ�
    public float duration = 0.5f;   // ����ʱ������0��1����ʱ�䣩
    public float spread = 0.5f;     // ÿ�ſ�������ƫ�Ƶľ���

    // ״̬��ǣ�true ��ʾչ����false ��ʾ�ջ�
    public bool opened = false;

    public Transform[] cards;
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    // ��ֵ���� 0 ��ʾ�ر�״̬��1 ��ʾչ��״̬
    private float transition = 0f;

    void Start()
    {
        // �������ʱ�Ѿ��������壬���ʼ������
        if (transform.childCount > 0)
            Initialize();
    }

    // ���ƺ���ã����ڻ��濨�Ƴ�ʼ״̬
    public void Initialize()
    {
        Debug.Log("Initialize");
        int count = transform.childCount;
        cards = new Transform[count];
        originalPositions = new Vector3[count];
        originalRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            cards[i] = transform.GetChild(i);
            originalPositions[i] = cards[i].localPosition;
            originalRotations[i] = cards[i].localRotation;
        }
    }

    // �ⲿ�ɵ��ã�����״̬Ϊչ��
    public void Open()
    {
        opened = true;
    }

    // �ⲿ�ɵ��ã�����״̬Ϊ�ջ�
    public void Close()
    {
        opened = false;
    }

    void Update()
    {
        if (cards == null || cards.Length == 0)
            return;

        // ���� desired state ���� transition ֵ��0��1֮�䣩
        if (opened)
        {
            transition += Time.deltaTime / duration;
        }
        else
        {
            transition -= Time.deltaTime / duration;
        }
        transition = Mathf.Clamp01(transition);

        int count = cards.Length;
        for (int i = 0; i < count; i++)
        {
            // ����Ŀ����ת�Ƕȣ��� fanAngle/2 �� -fanAngle/2����չ��ʱ�����ϲ��£���²�չ����
            float angle = fanAngle / 2 - (fanAngle / (count - 1)) * i;
            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            // ����������������ƫ�ƣ����м�Ŀ��Ʊ��ֲ���������ֱ��������ƶ�
            float offsetX = (i - (count - 1) / 2f) * spread;
            Vector3 targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

            // ͨ����ֵ���㵱ǰ״̬
            cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
            cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
        }
    }
}