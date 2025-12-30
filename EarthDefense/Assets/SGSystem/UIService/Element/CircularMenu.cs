using UnityEngine;
using UnityEngine.UI;

namespace SG.UI
{
    [ExecuteInEditMode] // ������ ��忡���� �ڵ� ����
    public class CircularMenu : MonoBehaviour
    {
        public RectTransform[] menuItems; // ��ġ�� UI �׸�� (��ư ��)
        public float radius = 100f; // ���� ������
        public float startAngle = -90f; // ���� ����
        public float endAngle = 90f; // �� ����
        public bool clockwise = true; // �ð� ���� ���� ����
        public bool IsEnableEditorUpdate = true; // �����Ϳ��� ������Ʈ ����
        public bool IsEveryTimeUpdate = false; // �� �����Ӹ��� ������Ʈ ����

        void Start()
        {
            UpdateMenu(); // ���� �� ������Ʈ
        }

        // �����Ϳ��� ���� ����� �� �ڵ� ����
        void OnValidate()
        {
            if( IsEnableEditorUpdate)
                UpdateMenu();
        }

        private void Update()
        {
            if( IsEveryTimeUpdate )
                UpdateMenu();
        }

        // UI�� �������� ��ġ�ϴ� �޼���
        void UpdateMenu()
        {
            if( menuItems == null || menuItems.Length == 0 ) return;

            int count = menuItems.Length;
            float angleStep = (count > 1) ? (endAngle - startAngle) / (count - 1) : 0;

            for( int i = 0; i < count; i++ )
            {
                float angle = startAngle + angleStep * i; // ��ġ�� ����
                if( !clockwise ) angle = endAngle - angleStep * i; // �ݽð� ���� ó��

                float rad = angle * Mathf.Deg2Rad; // ������ �������� ��ȯ
                float x = Mathf.Cos( rad ) * radius;
                float y = Mathf.Sin( rad ) * radius;

                if( menuItems[i] != null )
                    menuItems[i].anchoredPosition = new Vector2( x, y );
            }
        }
    }
}
