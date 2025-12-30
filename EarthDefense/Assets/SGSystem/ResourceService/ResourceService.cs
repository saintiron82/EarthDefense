using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

namespace SG.Resource
{
    public class ResourceService : ServiceBase
    {

        public class SpriteContainer
        {
            public string Category; // Category of the sprite, e.g., "Cat", "Goods", "Clothes", etc.
            public string Path;
            public string Name;
            public Sprite Sprite;
        }

        public static readonly string CatIconPath = "UI/Icon/CatIcon/";
        public static readonly string GoodsIconPath = "UI/Goods/Icon/";
        public static readonly string ClothesIconPath = "UI/Icon/Clothes/";
        public static readonly string FieldObjectIconPath = "UI/Icon/FieldObject/";
        static Dictionary<string, Texture2D> textureStorege = new Dictionary<string, Texture2D>();
        static Dictionary<string, GameObject> gameObjectStorege = new Dictionary<string, GameObject>();
        static Dictionary<string, List<SpriteContainer>> spriteContainerStorege = new Dictionary<string, List<SpriteContainer>>();
        
        protected override void InitInternal()
        {
            Addressables.InitializeAsync().WaitForCompletion();
            base.InitInternal();
        }

        public static Sprite LoadSprite(string path, string resName, bool Instantiate = true )
        {
            return LoadSprite( "Default", path, resName, Instantiate );
        }

        public override UniTask<bool> Init()
        {
            return base.Init();
        }

        public static Texture2D LoadTexture( string path, string resName)
        {
            var targetName = path + resName;
            var target = default(Texture2D);
            if(textureStorege.ContainsKey(targetName) )
            {
                target = textureStorege[targetName];
            } 
            else
            {
                var load = Resources.Load<Texture2D>( path + resName );
                if( load != null )
                {
                    target = load;
                    textureStorege.Add(targetName, load);
                }
            }
            if( target != null )
            {
               return target;
            }
            else
            {
                Debug.LogError($"Texture not found: {path + resName}");
                return default;
            }
        }

        public static Sprite LoadSprite( string container, string path, string resName, bool Instantiate = true )
        {
            if( container == null || container.Length <= 0 )
            {
                container = "Default"; // Default container if none specified
            }
            var target = default(Sprite);
            if( spriteContainerStorege.ContainsKey( container ) )
            {
                var containerList = spriteContainerStorege[container];
                foreach( var item in containerList )
                {
                    if( item.Name == resName )
                    {
                        target = item.Sprite;
                        break;
                    }
                }
            }
            if( target == null )
            {
                var load = Resources.Load<Sprite>( path + resName );
                if( load != null )
                {
                    target = load;
                    if( !spriteContainerStorege.ContainsKey( container ) )
                    {
                        spriteContainerStorege[container] = new List<SpriteContainer>();
                    }
                    spriteContainerStorege[container].Add( new SpriteContainer { Category = container, Path = path, Name = resName, Sprite = load } );
                }
            }
            if( target != null )
            {
                if( Instantiate )
                {
                    return GameObject.Instantiate(target);
                }
                else
                {
                    return target;
                }
            }
            else
            {
                Debug.LogError($"Sprite not found: {path + resName}");
                return default;
            }
        }

        public static GameObject LoadGameObject( string path, string resName )
        {
            var targetName = path + resName;
            var target = default(GameObject);
            if( gameObjectStorege.ContainsKey(targetName) )
            {
                target = gameObjectStorege[targetName];
            } else
            {
                var load = Resources.Load<GameObject>( path + resName );
                if( load != null )
                {
                    target = load;
                    gameObjectStorege.Add(targetName, load);
                }
            }

            if( target != null )
            {
                return GameObject.Instantiate(target);
            }
            else
            {
                return default;
            }
        }

        public static T LoadMonoObject<T>( string path, string resName ) where T : MonoBehaviour
        {
            var target = LoadGameObject( path, resName );
            if( target != null )
            {
                return target.GetComponent<T>();
            }
            else
            {
                return default;
            }
        }

        public static Transform LoadTransform( string path, string resName )
        {
            var target = LoadGameObject( path, resName );
            if( target != null )
            {
                return target.transform;
            }
            else
            {
                return default;
            }
        }

        public static Sprite GetSprite(params string[] addressPath)
        {
            if (addressPath == null || addressPath.Length == 0)
            {
                Debug.LogError("AddressPath 배열이 비어 있습니다.");
                return null;
            }
            return GetSprite(Path.Combine(addressPath).ToUnixPath());
        }

        public static Sprite GetSprite(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Addressable address가 비어 있습니다.");
                return null;
            }

            var fixedAddress = string.Format("Assets/AddressableResources/{0}.png", address).ToUnixPath();
            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(fixedAddress);
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"Addressable에서 Sprite를 로드하지 못했습니다: {fixedAddress}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Addressable Sprite 로드 중 예외 발생: {fixedAddress} - {ex.Message}");
                return null;
            }
        }

        public static AudioClip LoadAudioClip(params string[] addressPath)
        {
            if (addressPath == null || addressPath.Length == 0)
            {
                Debug.LogError("AddressPath 배열이 비어 있습니다.");
                return null;
            }
            return LoadAudioClip(Path.Combine(addressPath).ToUnixPath());
        }

        public static AudioClip LoadAudioClip(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Addressable address가 비어 있습니다.");
                return null;
            }
            var fixedAddress = string.Format("Assets/AddressableResources/BGM/{0}.ogg", address);
            try
            {
                var handle = Addressables.LoadAssetAsync<AudioClip>(fixedAddress);
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"Addressable에서 AudioClip을 로드하지 못했습니다: {fixedAddress}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Addressable AudioClip 로드 중 예외 발생: {fixedAddress} - {ex.Message}");
                return null;
            }
        }

        public static bool IsSpriteLoaded(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("Addressable address가 비어 있습니다.");
                return false;
            }
            var fixedAddress = string.Format("Assets/AddressableResources/{0}.png", address);
            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(fixedAddress);
                handle.WaitForCompletion();
                return handle.Status == AsyncOperationStatus.Succeeded;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Addressable Sprite 로드 중 예외 발생: {fixedAddress} - {ex.Message}");
                return false;
            }
        }

        public static Texture2D MergeTexture( Texture2D _a, Texture2D _b )
        {
            Texture2D finalTex = new Texture2D( _a.width, _a.height );
            Color[] colorArray = new Color[finalTex.width * finalTex.height];
            Color[][] srcArray = new Color[2][];

            srcArray[0] = _a.GetPixels();
            srcArray[1] = _b.GetPixels();


            for( int x = 0; x < finalTex.width; ++x )
            {
                for( int y = 0; y < finalTex.height; ++y )
                {
                    int pixelIndex = x + y * finalTex.width;
                    for( int i = 0; i < 2; ++i )
                    {
                        Color srcPixel = srcArray[i][pixelIndex];
                        if( srcPixel.a > 0.5f )
                            colorArray[pixelIndex] = srcPixel;
                    }
                }
            }

            finalTex.SetPixels( colorArray );
            finalTex.Apply();

            finalTex.wrapMode = TextureWrapMode.Clamp;
            finalTex.filterMode = FilterMode.Bilinear;
            //finalTex.minimumMipmapLevel = 0;
            return finalTex;
        }

        public static void MeshExtend_Head( Mesh _mesh, float _fatFactor )
        {
            //Vector3 center = new Vector3(0, 0.01f, -0.01f);
            var originalVertices = _mesh.vertices;
            int max = originalVertices.Length;

            Vector3[] deformVertices = new Vector3[max];
            float min = _fatFactor * 0.001f;
            for( int i = 0; i < max; ++i )
            {
                var v = originalVertices[i];
                var sqrMag = v.sqrMagnitude;
                float d = _fatFactor;
                if( sqrMag > 0.001f )
                    d = min / (sqrMag * sqrMag);

                d = Mathf.Min( d, 0.3f );
                v.y = 0;
                //forceDir.y = 0;
                deformVertices[i] = originalVertices[i] + v * d;
            }
            _mesh.vertices = deformVertices;
            //_mesh.RecalculateNormals
        }

        // 커서 텍스처를 불러올 때 Read/Write 설정이 되어 있는지 확인하는 메서드 예시 추가
        public static Texture2D LoadCursorTexture(string folder, string name)
        {
            var tex = LoadTexture(folder, name);
            if (tex != null && !tex.isReadable)
            { 
                Debug.LogWarning($"커서 텍스처 '{name}'가 CPU에서 접근 불가합니다. Unity Import Settings에서 'Read/Write Enabled'를 켜세요.");
            }
            return tex;
        }
    }
}

