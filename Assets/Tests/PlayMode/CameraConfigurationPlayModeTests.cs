using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CameraConfigurationPlayModeTests
{
    private const string ArenaScenePath = "Assets/Scenes/arena 1.unity";

    [UnitySetUp]
    public IEnumerator LoadArenaScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        yield return null;
    }

    [Test]
    public void ArenaScene_HasExactlyOneMainCameraAndAudioListener()
    {
        Camera[] mainTaggedCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(camera => camera.enabled && camera.CompareTag("MainCamera"))
            .ToArray();
        AudioListener[] audioListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(listener => listener.enabled)
            .ToArray();

        Assert.That(Camera.main, Is.Not.Null);
        Assert.That(mainTaggedCameras.Length, Is.EqualTo(1));
        Assert.That(audioListeners.Length, Is.EqualTo(1));
    }

    [Test]
    public void ArenaScene_MainCameraAndFollowCameraKeepSeparateRoles()
    {
        Camera mainCamera = Camera.main;
        GameObject followCamera = GameObject.Find("Player Follow Camera");

        Assert.That(mainCamera, Is.Not.Null);
        Assert.That(mainCamera.gameObject.name, Is.EqualTo("Main Camera"));
        Assert.That(mainCamera.GetComponent<AudioListener>(), Is.Not.Null);
        Assert.That(mainCamera.GetComponent<CinemachinePlatformerCamera>(), Is.Not.Null);

        Assert.That(followCamera, Is.Not.Null);
        Assert.That(followCamera.GetComponent<Camera>(), Is.Null);
        Assert.That(followCamera.GetComponent<AudioListener>(), Is.Null);
        Assert.That(
            followCamera.GetComponents<Behaviour>().Any(component => component.GetType().Name.Contains("Cinemachine")),
            Is.True);
    }
}
