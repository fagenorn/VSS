using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace Assets.Scripts.Room
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class RoomBackground : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Resize();
        }

        private void Resize()
        {
            if (_spriteRenderer == null) return;

            transform.localScale = new Vector3(1, 1, 1);

            float width = _spriteRenderer.sprite.bounds.size.x;
            float height = _spriteRenderer.sprite.bounds.size.y;

            float worldScreenHeight = Camera.main.orthographicSize * 2f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            Vector3 xWidth = transform.localScale;
            xWidth.x = worldScreenWidth / width;
            transform.localScale = xWidth;

            Vector3 yHeight = transform.localScale;
            yHeight.y = worldScreenHeight / height;
            transform.localScale = yHeight;
        }
    }
}
