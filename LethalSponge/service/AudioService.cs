using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;

namespace Scoops.service
{
    public class AudioInfo
    {
        public string name;
        public float length;
        public int channels;
        public int frequency;

        public AudioInfo(AudioClip audio)
        {
            this.name = audio.name;
            this.length = audio.length;
            this.channels = audio.channels;
            this.frequency = audio.frequency;
        }

        public override bool Equals(object obj) => this.Equals(obj as AudioClip);

        public bool Equals(AudioClip a)
        {
            if (a is null)
            {
                return false;
            }
            if (System.Object.ReferenceEquals(this, a))
            {
                return true;
            }
            if (this.GetType() != a.GetType())
            {
                return false;
            }

            return (name == a.name) && (length == a.length) && (channels == a.channels) && (frequency == a.frequency);
        }

        public override int GetHashCode() => (name, length, channels, frequency).GetHashCode();

        public static bool operator ==(AudioInfo lhs, AudioInfo rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(AudioInfo lhs, AudioInfo rhs) => !(lhs == rhs);
    }

    public static class AudioService
    {
        public static Dictionary<AudioInfo, AudioClip> AudioDict = new Dictionary<AudioInfo, AudioClip>();
        public static List<AudioClip> dupedAudio = new List<AudioClip>();

        public static string[] deDupeBlacklist;

        public static void DedupeAllAudio()
        {
            // Here we go through every possible location an audio clip can be saved in base lethal company

            AudioSource[] allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (AudioSource audioSource in allAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.clip = DedupeAudio(audioSource.clip);
                }
            }
            
            AnimatedObjectTrigger[] allAnimatedObjectTriggers = Resources.FindObjectsOfTypeAll<AnimatedObjectTrigger>();
            foreach (AnimatedObjectTrigger animatedObjectTrigger in allAnimatedObjectTriggers)
            {
                if (animatedObjectTrigger != null)
                {
                    for (int i = 0; i < animatedObjectTrigger.boolTrueAudios.Length; i++)
                    {
                        animatedObjectTrigger.boolTrueAudios[i] = DedupeAudio(animatedObjectTrigger.boolTrueAudios[i]);
                    }
                    for (int i = 0; i < animatedObjectTrigger.boolFalseAudios.Length; i++)
                    {
                        animatedObjectTrigger.boolFalseAudios[i] = DedupeAudio(animatedObjectTrigger.boolFalseAudios[i]);
                    }
                    for (int i = 0; i < animatedObjectTrigger.secondaryAudios.Length; i++)
                    {
                        animatedObjectTrigger.secondaryAudios[i] = DedupeAudio(animatedObjectTrigger.secondaryAudios[i]);
                    }
                }
            }
            
            EntranceTeleport[] allEntranceTeleports = Resources.FindObjectsOfTypeAll<EntranceTeleport>();
            foreach (EntranceTeleport entranceTeleport in allEntranceTeleports)
            {
                if (entranceTeleport != null)
                {
                    for (int i = 0; i < entranceTeleport.doorAudios.Length; i++)
                    {
                        entranceTeleport.doorAudios[i] = DedupeAudio(entranceTeleport.doorAudios[i]);
                    }
                }
            }
            
            DoorLock[] allDoorLocks = Resources.FindObjectsOfTypeAll<DoorLock>();
            foreach (DoorLock doorLock in allDoorLocks)
            {
                if (doorLock != null)
                {
                    doorLock.pickingLockSFX = DedupeAudio(doorLock.pickingLockSFX);
                    doorLock.unlockSFX = DedupeAudio(doorLock.unlockSFX);
                }
            }
            
            PlayAudioAnimationEvent[] allPlayAudioAnimationEvents = Resources.FindObjectsOfTypeAll<PlayAudioAnimationEvent>();
            foreach (PlayAudioAnimationEvent playAudioAnimationEvent in allPlayAudioAnimationEvents)
            {
                if (playAudioAnimationEvent != null)
                {
                    playAudioAnimationEvent.audioClip = DedupeAudio(playAudioAnimationEvent.audioClip);
                    playAudioAnimationEvent.audioClip2 = DedupeAudio(playAudioAnimationEvent.audioClip2);
                    playAudioAnimationEvent.audioClip3 = DedupeAudio(playAudioAnimationEvent.audioClip3);
                    for (int i = 0; i < playAudioAnimationEvent.randomClips.Length; i++)
                    {
                        playAudioAnimationEvent.randomClips[i] = DedupeAudio(playAudioAnimationEvent.randomClips[i]);
                    }
                    for (int i = 0; i < playAudioAnimationEvent.randomClips2.Length; i++)
                    {
                        playAudioAnimationEvent.randomClips2[i] = DedupeAudio(playAudioAnimationEvent.randomClips2[i]);
                    }
                }
            }
            
            EnemyVent[] allEnemyVents = Resources.FindObjectsOfTypeAll<EnemyVent>();
            foreach (EnemyVent enemyVent in allEnemyVents)
            {
                if (enemyVent != null)
                {
                    enemyVent.ventCrawlSFX = DedupeAudio(enemyVent.ventCrawlSFX);
                }
            }
            
            LungProp[] allLungProps = Resources.FindObjectsOfTypeAll<LungProp>();
            foreach (LungProp lungProp in allLungProps)
            {
                if (lungProp != null)
                {
                    lungProp.connectSFX = DedupeAudio(lungProp.connectSFX);
                    lungProp.disconnectSFX = DedupeAudio(lungProp.disconnectSFX);
                    lungProp.removeFromMachineSFX = DedupeAudio(lungProp.removeFromMachineSFX);
                }
            }
            
            Item[] allItems = Resources.FindObjectsOfTypeAll<Item>();
            foreach (Item item in allItems)
            {
                if (item != null)
                {
                    item.grabSFX = DedupeAudio(item.grabSFX);
                    item.dropSFX = DedupeAudio(item.dropSFX);
                    item.pocketSFX = DedupeAudio(item.pocketSFX);
                    item.throwSFX = DedupeAudio(item.throwSFX);
                }
            }
            
            LevelAmbienceLibrary[] allLevelAmbienceLibraries = Resources.FindObjectsOfTypeAll<LevelAmbienceLibrary>();
            foreach (LevelAmbienceLibrary levelAmbienceLibrary in allLevelAmbienceLibraries)
            {
                if (levelAmbienceLibrary != null)
                {
                    for (int i = 0; i < levelAmbienceLibrary.insanityMusicAudios.Length; i++)
                    {
                        levelAmbienceLibrary.insanityMusicAudios[i] = DedupeAudio(levelAmbienceLibrary.insanityMusicAudios[i]);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.insideAmbience.Length; i++)
                    {
                        levelAmbienceLibrary.insideAmbience[i] = DedupeAudio(levelAmbienceLibrary.insideAmbience[i]);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.shipAmbience.Length; i++)
                    {
                        levelAmbienceLibrary.shipAmbience[i] = DedupeAudio(levelAmbienceLibrary.shipAmbience[i]);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.outsideAmbience.Length; i++)
                    {
                        levelAmbienceLibrary.outsideAmbience[i] = DedupeAudio(levelAmbienceLibrary.outsideAmbience[i]);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.insideAmbienceInsanity.Length; i++)
                    {
                        levelAmbienceLibrary.insideAmbienceInsanity[i].audioClip = DedupeAudio(levelAmbienceLibrary.insideAmbienceInsanity[i].audioClip);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.shipAmbienceInsanity.Length; i++)
                    {
                        levelAmbienceLibrary.shipAmbienceInsanity[i].audioClip = DedupeAudio(levelAmbienceLibrary.shipAmbienceInsanity[i].audioClip);
                    }
                    for (int i = 0; i < levelAmbienceLibrary.outsideAmbienceInsanity.Length; i++)
                    {
                        levelAmbienceLibrary.outsideAmbienceInsanity[i].audioClip = DedupeAudio(levelAmbienceLibrary.outsideAmbienceInsanity[i].audioClip);
                    }
                }
            }
            
            RadMechAI[] allRadMechAIs = Resources.FindObjectsOfTypeAll<RadMechAI>();
            foreach (RadMechAI radMechAI in allRadMechAIs)
            {
                if (radMechAI != null)
                {
                    radMechAI.spotlightOff = DedupeAudio(radMechAI.spotlightOff);
                    radMechAI.spotlightFlicker = DedupeAudio(radMechAI.spotlightFlicker);
                    for (int i = 0; i < radMechAI.shootGunSFX.Length; i++)
                    {
                        radMechAI.shootGunSFX[i] = DedupeAudio(radMechAI.shootGunSFX[i]);
                    }
                    for (int i = 0; i < radMechAI.largeExplosionSFX.Length; i++)
                    {
                        radMechAI.largeExplosionSFX[i] = DedupeAudio(radMechAI.largeExplosionSFX[i]);
                    }
                }
            }
            
            Shovel[] allShovels = Resources.FindObjectsOfTypeAll<Shovel>();
            foreach (Shovel shovel in allShovels)
            {
                if (shovel != null)
                {
                    shovel.reelUp = DedupeAudio(shovel.reelUp);
                    shovel.swing = DedupeAudio(shovel.swing);
                    for (int i = 0; i < shovel.hitSFX.Length; i++)
                    {
                        shovel.hitSFX[i] = DedupeAudio(shovel.hitSFX[i]);
                    }
                }
            }
            
            KnifeItem[] allKnifeItems = Resources.FindObjectsOfTypeAll<KnifeItem>();
            foreach (KnifeItem knifeItem in allKnifeItems)
            {
                if (knifeItem != null)
                {
                    for (int i = 0; i < knifeItem.hitSFX.Length; i++)
                    {
                        knifeItem.hitSFX[i] = DedupeAudio(knifeItem.hitSFX[i]);
                    }
                    for (int i = 0; i < knifeItem.swingSFX.Length; i++)
                    {
                        knifeItem.swingSFX[i] = DedupeAudio(knifeItem.swingSFX[i]);
                    }
                }
            }
            
            PlaceableShipObject[] allPlaceableShipObjects = Resources.FindObjectsOfTypeAll<PlaceableShipObject>();
            foreach (PlaceableShipObject placeableShipObject in allPlaceableShipObjects)
            {
                if (placeableShipObject != null)
                {
                    placeableShipObject.placeObjectSFX = DedupeAudio(placeableShipObject.placeObjectSFX);
                }
            }
            
            EnemyType[] allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (EnemyType enemyType in allEnemyTypes)
            {
                if (enemyType != null)
                {
                    enemyType.overrideVentSFX = DedupeAudio(enemyType.overrideVentSFX);
                    enemyType.hitBodySFX = DedupeAudio(enemyType.hitBodySFX);
                    enemyType.hitEnemyVoiceSFX = DedupeAudio(enemyType.hitEnemyVoiceSFX);
                    enemyType.stunSFX = DedupeAudio(enemyType.stunSFX);
                    for (int i = 0; i < enemyType.audioClips.Length; i++)
                    {
                        enemyType.audioClips[i] = DedupeAudio(enemyType.audioClips[i]);
                    }
                }
            }
            
            FlashlightItem[] allFlashlightItems = Resources.FindObjectsOfTypeAll<FlashlightItem>();
            foreach (FlashlightItem flashlightItem in allFlashlightItems)
            {
                if (flashlightItem != null)
                {
                    flashlightItem.outOfBatteriesClip = DedupeAudio(flashlightItem.outOfBatteriesClip);
                    flashlightItem.flashlightFlicker = DedupeAudio(flashlightItem.flashlightFlicker);
                    for (int i = 0; i < flashlightItem.flashlightClips.Length; i++)
                    {
                        flashlightItem.flashlightClips[i] = DedupeAudio(flashlightItem.flashlightClips[i]);
                    }
                }
            }
            
            NoisemakerProp[] allNoisemakerProps = Resources.FindObjectsOfTypeAll<NoisemakerProp>();
            foreach (NoisemakerProp noisemakerProp in allNoisemakerProps)
            {
                if (noisemakerProp != null)
                {
                    for (int i = 0; i < noisemakerProp.noiseSFX.Length; i++)
                    {
                        noisemakerProp.noiseSFX[i] = DedupeAudio(noisemakerProp.noiseSFX[i]);
                    }
                    for (int i = 0; i < noisemakerProp.noiseSFXFar.Length; i++)
                    {
                        noisemakerProp.noiseSFXFar[i] = DedupeAudio(noisemakerProp.noiseSFXFar[i]);
                    }
                }
            }

            //foreach (AudioClip dupedAudio in dupedAudio)
            //{
            //    AudioClip.Destroy(dupedAudio);
            //    Resources.UnloadAsset(dupedAudio);
            //}

            dupedAudio.Clear();
            AudioDict.Clear();
        }

        public static AudioClip DedupeAudio(AudioClip clip)
        {
            if (clip == null) return null;

            AudioInfo info = new AudioInfo(clip);

            if (AudioDict.TryGetValue(info, out AudioClip processedAudio))
            {
                if (processedAudio.GetInstanceID() == clip.GetInstanceID())
                {
                    // Already processed
                    return clip;
                }
                else
                {
                    dupedAudio.Add(clip);
                    return processedAudio;
                }
            }
            else
            {
                AddToAudioDict(info, clip);
                return clip;
            }
        }

        public static void AddToAudioDict(AudioInfo info, AudioClip audio)
        {
            if (info.name == "" || deDupeBlacklist.Contains(info.name.ToLower())) return;
            AudioDict.Add(info, audio);
        }
    }
}
