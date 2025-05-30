using System;
using UnityEngine;
using System.Collections;

public class CardScratcher : MonoBehaviour
{
    public Material mat;
    public RectTransform mask;    
    public Material maskMat;    
    [Range(0f,1)]
    public float scratchRadius = 0.1f;
    private float _maskWidth;
    private float _maskHeight;
    // [Header("进度计算频率，单位:帧")]
    // public float progressUpdateRate = 1;

    private Vector2 _lastTouchPos;
    private bool _isTouching = false;
    private bool _isFirstTouch = true;

    private float _aspect = 1f;
    private float _progress = 1f;
    private bool _isInAera = false;
    public float Progress => _progress;

    public event Action OnScratchStart; 
    public event Action<float> OnProgressChange; 
    private RenderTexture _texture;
    private RenderTexture _textureTemp;

    /// <summary>
    /// 计算刮卡进度
    /// </summary>
    public void CalcProgress()
    {
        var texture2D = RenderTexture2Texture2D(_texture);
        var colorArray = texture2D.GetPixels(texture2D.mipmapCount - 1);
        
        if (colorArray.Length == 1)
        {
            _progress = 1 - colorArray[0].a;
        }
        else
        {
            _progress = 1;
        }
        Destroy(texture2D);
        OnProgressChange?.Invoke(_progress);
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Start()
    {
        _maskWidth = mask.rect.width;
        _maskHeight = mask.rect.height;
        _aspect = _maskWidth / _maskHeight;

        _texture = new RenderTexture((int)_maskWidth, (int)_maskHeight, 0, RenderTextureFormat.Default);
        _textureTemp = new RenderTexture((int)_maskWidth, (int)_maskHeight, 0, RenderTextureFormat.Default);
        _texture.autoGenerateMips = true;
        maskMat.SetTexture("_Mask", _texture);
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // _tmpTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // _tmpTouchPos.z = 0;

            OnMouseDown(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(OnPostRender());//仅在抬手时计算进度
            _isTouching = false;
        }


    }

    private void OnMouseDown(Vector2 touchPos)
    {

        //计算点击点相对uv位置

        Vector2 uvPos  = Vector2.zero * 100;

        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(mask,touchPos,Camera.main,out uvPos))
        {
            // Debug.Log($"=======IN{uvPos}");            
            uvPos.x = (uvPos.x + 0.5f*_maskWidth) / _maskWidth;
            uvPos.y = (uvPos.y +0.5f*_maskHeight) / _maskHeight;
            Debug.Log($"=======uvPos{uvPos}");

            _isInAera = uvPos.x >= 0 && uvPos.x <= 1 && uvPos.y <= 1 && uvPos.y >= 0;
            if (_isInAera && _isFirstTouch)
            {
                _isFirstTouch = false;
                OnScratchStart?.Invoke();
            }
        }

        if (!_isTouching)
        {
            _isTouching = true;
            _lastTouchPos = uvPos;
            mat.SetFloat("_HoleRadius", scratchRadius);
        }

        mat.SetFloat("_HoleCenterX", uvPos.x);
        mat.SetFloat("_HoleCenterY", uvPos.y);
        mat.SetFloat("_LastHoleCenterX", _lastTouchPos.x);
        mat.SetFloat("_LastHoleCenterY", _lastTouchPos.y);
        mat.SetFloat("_Aspect", _aspect);
        _lastTouchPos = uvPos;
        //TODO 改成DoubleBuffer
        Graphics.Blit(_texture, _textureTemp);
        Graphics.Blit(_textureTemp, _texture, mat, 0);
        
    }

    private IEnumerator OnPostRender()
    {
        //TODO 间隔调用
        if (_isTouching)
        {
            yield return new WaitForEndOfFrame();
            Debug.Log($"ClearProgress:{_progress}");
            CalcProgress();
        }

    }

    public Texture2D RenderTexture2Texture2D(RenderTexture rt)
    {
        RenderTexture preRT = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = preRT;
        return tex;
    }

    public void Clear()
    {
        Debug.Log($"=======Clear");
        
        Graphics.Blit(null, _texture, mat, 1);//清空RT
        _isTouching = false;
        _isFirstTouch = true;
        _progress = 1f;
    }

    private void OnDestroy()
    {
        Destroy(_texture);
        Destroy(_textureTemp);
    }
}
