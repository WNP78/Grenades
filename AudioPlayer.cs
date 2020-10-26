namespace WNP78.Grenades
{
    using UnityEngine;

    public static class AudioPlayer
    {
        public static void PlayAtPoint(AudioClip clip, Vector3 position, float? volume)
        {
            var go = GlobalPool.Spawn("AudioPlayer", position, Quaternion.identity);
            var player = global::AudioPlayer.Cache.Get(go);
            var source = player.source;
            player._hasStartedPlaying = true;
            source.Stop();
            source.clip = clip;
            source.timeSamples = 0;
            source.outputAudioMixerGroup = player._defaultMixerGroup;
            source.volume = volume ?? player._defaultVolume;
            source.loop = false;
            source.pitch = 1f;
            source.minDistance = player._defaultMinDistance;
            source.Play();
            MelonLoader.MelonLogger.Log($"volume: {player._defaultVolume}, minDist: {source.minDistance}");
        }
    }
}
