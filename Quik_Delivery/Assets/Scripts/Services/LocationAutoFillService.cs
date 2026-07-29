using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// 버튼 클릭 시 1회만 현재 위치를 가져와 주소로 변환하는 간단한 서비스.
/// 실시간 추적은 하지 않는다.
/// </summary>
public class LocationAutoFillService : MonoBehaviour
{
    [Serializable]
    public class LocationResult
    {
        public bool IsSuccess = false;
        public string Address = string.Empty;
        public double Latitude = 0d;
        public double Longitude = 0d;
        public string ErrorMessage = string.Empty;
    }

    [Header("Location Settings")]
    [SerializeField] private float desiredAccuracyInMeters = 20f;
    [SerializeField] private float updateDistanceInMeters = 10f;
    [SerializeField] private int initializeTimeoutSeconds = 15;

    /// <summary>
    /// 현재 위치를 1회 받아 주소 문자열로 변환한다.
    /// </summary>
    public IEnumerator RequestCurrentAddress(Action<LocationResult> onCompleted)
    {
        LocationResult result = new LocationResult();

#if UNITY_EDITOR
        result.IsSuccess = true;
        result.Address = "에디터 테스트 위치";
        result.Latitude = 37.5665d;
        result.Longitude = 126.9780d;
        onCompleted?.Invoke(result);
        yield break;
#else
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);

            float permissionWait = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && permissionWait < 10f)
            {
                permissionWait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                result.ErrorMessage = "위치 권한이 허용되지 않았습니다";
                onCompleted?.Invoke(result);
                yield break;
            }
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            result.ErrorMessage = "기기 위치 서비스가 꺼져 있습니다";
            onCompleted?.Invoke(result);
            yield break;
        }

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        int waitCount = initializeTimeoutSeconds;
        while (Input.location.status == LocationServiceStatus.Initializing && waitCount > 0)
        {
            yield return new WaitForSeconds(1f);
            waitCount -= 1;
        }

        if (waitCount <= 0)
        {
            result.ErrorMessage = "위치 확인 시간이 초과되었습니다";
            Input.location.Stop();
            onCompleted?.Invoke(result);
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            result.ErrorMessage = "현재 위치를 가져오지 못했습니다";
            Input.location.Stop();
            onCompleted?.Invoke(result);
            yield break;
        }

        LocationInfo lastData = Input.location.lastData;
        result.Latitude = lastData.latitude;
        result.Longitude = lastData.longitude;

        string resolvedAddress = ResolveAddress(result.Latitude, result.Longitude);

        if (string.IsNullOrWhiteSpace(resolvedAddress))
        {
            result.Address = $"{result.Latitude:F5}, {result.Longitude:F5}";
            result.ErrorMessage = "주소 변환에 실패하여 좌표로 저장했습니다";
        }
        else
        {
            result.Address = resolvedAddress;
        }

        result.IsSuccess = true;
        Input.location.Stop();
        onCompleted?.Invoke(result);
#endif
    }

    private string ResolveAddress(double latitude, double longitude)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return ResolveAddressAndroid(latitude, longitude);
#else
        return string.Empty;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private string ResolveAddressAndroid(double latitude, double longitude)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
            using (AndroidJavaObject locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
            using (AndroidJavaObject geocoder = new AndroidJavaObject("android.location.Geocoder", currentActivity, locale))
            {
                bool isPresent = geocoder.CallStatic<bool>("isPresent");
                if (!isPresent)
                {
                    return string.Empty;
                }

                using (AndroidJavaObject addressList = geocoder.Call<AndroidJavaObject>("getFromLocation", latitude, longitude, 1))
                {
                    if (addressList == null)
                    {
                        return string.Empty;
                    }

                    int size = addressList.Call<int>("size");
                    if (size <= 0)
                    {
                        return string.Empty;
                    }

                    using (AndroidJavaObject address = addressList.Call<AndroidJavaObject>("get", 0))
                    {
                        string addressLine = address.Call<string>("getAddressLine", 0);
                        if (!string.IsNullOrWhiteSpace(addressLine))
                        {
                            return addressLine.Trim();
                        }

                        List<string> parts = new List<string>();
                        AddAddressPart(parts, address.Call<string>("getAdminArea"));
                        AddAddressPart(parts, address.Call<string>("getSubAdminArea"));
                        AddAddressPart(parts, address.Call<string>("getLocality"));
                        AddAddressPart(parts, address.Call<string>("getSubLocality"));
                        AddAddressPart(parts, address.Call<string>("getThoroughfare"));
                        AddAddressPart(parts, address.Call<string>("getFeatureName"));

                        return string.Join(" ", parts).Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocationAutoFillService] 주소 변환 실패: {ex.Message}");
            return string.Empty;
        }
    }

    private void AddAddressPart(List<string> parts, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string trimmed = value.Trim();
        if (!parts.Contains(trimmed))
        {
            parts.Add(trimmed);
        }
    }
#endif
}
