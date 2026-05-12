Shader "Custom/CRTMonitor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Яркость", Range(0, 2)) = 1.0
        _ScanLineIntensity ("Интенсивность линий развертки", Range(0, 1)) = 0.5
        _Flicker ("Мерцание", Range(0, 1)) = 0.1
        _Vignette ("Затемнение краев", Range(0, 1)) = 0.3
        
        // Параметры шума
        _NoiseIntensity ("Интенсивность шума", Range(0, 1)) = 0.15
        _NoiseScale ("Масштаб шума", Range(1, 100)) = 50
        _NoiseSpeed ("Скорость шума", Range(1, 20)) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Brightness;
            float _ScanLineIntensity;
            float _Flicker;
            float _Vignette;
            float _NoiseIntensity;
            float _NoiseScale;
            float _NoiseSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Функция для генерации псевдослучайного шума
            float random(float2 uv)
            {
                // frac возвращает дробную часть
                // dot делает скалярное произведение с каким-то вектором
                // sin создаёт вариацию
                // 43758.5453 - волшебное число для лучшего распределения
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Функция для белого шума, зависящего от времени
            float whiteNoise(float2 uv, float time)
            {
                // Несколько слоёв шума с разными скоростями для более естественной картины
                float noise = random(uv + time * _NoiseSpeed * 0.1) * 0.5;
                noise += random(uv * 2.0 + time * _NoiseSpeed * 0.3) * 0.3;
                noise += random(uv * 4.0 - time * _NoiseSpeed * 0.7) * 0.2;
                return noise;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Дискретизируем UV для более крупного зерна
                float2 pixelUV = floor(i.uv * _NoiseScale) / _NoiseScale;
                
                // 1. Генерируем шум
                float noise = whiteNoise(pixelUV, _Time.y);
                
                // 2. Получаем исходный цвет
                fixed4 col = tex2D(_MainTex, i.uv);

                // 3. Накладываем шум (добавление или умножение - зависит от желаемого эффекта)
                col.rgb = lerp(col.rgb, col.rgb * noise, _NoiseIntensity);

                // 4. Мерцание (изменение яркости во времени)
                float timeFactor = _Time.y * 20.0;
                float flicker = _Flicker * (0.5 + 0.5 * sin(timeFactor + i.uv.y * 50.0));
                col.rgb *= (1.0 - flicker);

                // 5. Сканирующие линии
                float scanline = sin(i.uv.y * 200.0 + _Time.y * 5.0);
                scanline = abs(scanline);
                float lineEffect = 1.0 - _ScanLineIntensity * (1.0 - scanline);
                col.rgb *= lineEffect;

                // 6. Виньетка
                float2 dist = i.uv - 0.5;
                float vignette = 1.0 - dot(dist, dist) * _Vignette * 4.0;
                vignette = saturate(vignette);
                col.rgb *= vignette;

                // 7. Яркость
                col.rgb *= _Brightness;

                return col;
            }
            ENDCG
        }
    }
}