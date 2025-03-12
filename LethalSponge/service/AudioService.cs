using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Scoops.service
{
    public static class AudioService
    {
        public static Dictionary<string, AudioClip> AudioDict = new Dictionary<string, AudioClip>();
        public static List<AudioClip> dupedAudio = new List<AudioClip>();

        public static void DedupeAllAudio()
        {
            // Here we go through every possible location an audio clip can be saved in base lethal company

            AudioSource[] allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
            foreach (AudioSource audioSource in allAudioSources)
            {
                if (audioSource != null)
                {
                    DedupeAudio(audioSource.clip);
                }
            }

            AnimatedObjectTrigger[] allAnimatedObjectTriggers = Resources.FindObjectsOfTypeAll<AnimatedObjectTrigger>();
            foreach (AnimatedObjectTrigger animatedObjectTrigger in allAnimatedObjectTriggers)
            {
                if (animatedObjectTrigger != null)
                {
                    foreach (AudioClip clip in animatedObjectTrigger.boolTrueAudios)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in animatedObjectTrigger.boolFalseAudios)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in animatedObjectTrigger.secondaryAudios)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            EntranceTeleport[] allEntranceTeleports = Resources.FindObjectsOfTypeAll<EntranceTeleport>();
            foreach (EntranceTeleport entranceTeleport in allEntranceTeleports)
            {
                if (entranceTeleport != null)
                {
                    foreach (AudioClip clip in entranceTeleport.doorAudios)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            DoorLock[] allDoorLocks = Resources.FindObjectsOfTypeAll<DoorLock>();
            foreach (DoorLock doorLock in allDoorLocks)
            {
                if (doorLock != null)
                {
                    DedupeAudio(doorLock.pickingLockSFX);
                    DedupeAudio(doorLock.unlockSFX);
                }
            }

            PlayAudioAnimationEvent[] allPlayAudioAnimationEvents = Resources.FindObjectsOfTypeAll<PlayAudioAnimationEvent>();
            foreach (PlayAudioAnimationEvent playAudioAnimationEvent in allPlayAudioAnimationEvents)
            {
                if (playAudioAnimationEvent != null)
                {
                    DedupeAudio(playAudioAnimationEvent.audioClip);
                    DedupeAudio(playAudioAnimationEvent.audioClip2);
                    DedupeAudio(playAudioAnimationEvent.audioClip3);
                    foreach (AudioClip clip in playAudioAnimationEvent.randomClips)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in playAudioAnimationEvent.randomClips2)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            EnemyVent[] allEnemyVents = Resources.FindObjectsOfTypeAll<EnemyVent>();
            foreach (EnemyVent enemyVent in allEnemyVents)
            {
                if (enemyVent != null)
                {
                    DedupeAudio(enemyVent.ventCrawlSFX);
                }
            }

            LungProp[] allLungProps = Resources.FindObjectsOfTypeAll<LungProp>();
            foreach (LungProp lungProp in allLungProps)
            {
                if (lungProp != null)
                {
                    DedupeAudio(lungProp.connectSFX);
                    DedupeAudio(lungProp.disconnectSFX);
                    DedupeAudio(lungProp.removeFromMachineSFX);
                }
            }

            Item[] allItems = Resources.FindObjectsOfTypeAll<Item>();
            foreach (Item item in allItems)
            {
                if (item != null)
                {
                    DedupeAudio(item.grabSFX);
                    DedupeAudio(item.dropSFX);
                    DedupeAudio(item.pocketSFX);
                    DedupeAudio(item.throwSFX);
                }
            }

            LevelAmbienceLibrary[] allLevelAmbienceLibraries = Resources.FindObjectsOfTypeAll<LevelAmbienceLibrary>();
            foreach (LevelAmbienceLibrary levelAmbienceLibrary in allLevelAmbienceLibraries)
            {
                if (levelAmbienceLibrary != null)
                {
                    foreach (AudioClip clip in levelAmbienceLibrary.insanityMusicAudios)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in levelAmbienceLibrary.insideAmbience)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in levelAmbienceLibrary.shipAmbience)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in levelAmbienceLibrary.outsideAmbience)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (RandomAudioClip randomClip in levelAmbienceLibrary.insideAmbienceInsanity)
                    {
                        DedupeAudio(randomClip.audioClip);
                    }
                    foreach (RandomAudioClip randomClip in levelAmbienceLibrary.shipAmbienceInsanity)
                    {
                        DedupeAudio(randomClip.audioClip);
                    }
                    foreach (RandomAudioClip randomClip in levelAmbienceLibrary.outsideAmbienceInsanity)
                    {
                        DedupeAudio(randomClip.audioClip);
                    }
                }
            }

            RadMechAI[] allRadMechAIs = Resources.FindObjectsOfTypeAll<RadMechAI>();
            foreach (RadMechAI radMechAI in allRadMechAIs)
            {
                if (radMechAI != null)
                {
                    DedupeAudio(radMechAI.spotlightOff);
                    DedupeAudio(radMechAI.spotlightFlicker);
                    foreach (AudioClip clip in radMechAI.shootGunSFX)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in radMechAI.largeExplosionSFX)
                    {
                        DedupeAudio(clip);
                    }
                    
                }
            }

            Shovel[] allShovels = Resources.FindObjectsOfTypeAll<Shovel>();
            foreach (Shovel shovel in allShovels)
            {
                if (shovel != null)
                {
                    DedupeAudio(shovel.reelUp);
                    DedupeAudio(shovel.swing);
                    foreach (AudioClip clip in shovel.hitSFX)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            KnifeItem[] allKnifeItems = Resources.FindObjectsOfTypeAll<KnifeItem>();
            foreach (KnifeItem knifeItem in allKnifeItems)
            {
                if (knifeItem != null)
                {
                    foreach (AudioClip clip in knifeItem.hitSFX)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in knifeItem.swingSFX)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            PlaceableShipObject[] allPlaceableShipObjects = Resources.FindObjectsOfTypeAll<PlaceableShipObject>();
            foreach (PlaceableShipObject placeableShipObject in allPlaceableShipObjects)
            {
                if (placeableShipObject != null)
                {
                    DedupeAudio(placeableShipObject.placeObjectSFX);
                }
            }

            EnemyType[] allEnemyTypes = Resources.FindObjectsOfTypeAll<EnemyType>();
            foreach (EnemyType enemyType in allEnemyTypes)
            {
                if (enemyType != null)
                {
                    DedupeAudio(enemyType.overrideVentSFX);
                    DedupeAudio(enemyType.hitBodySFX);
                    DedupeAudio(enemyType.hitEnemyVoiceSFX);
                    DedupeAudio(enemyType.stunSFX);
                    foreach (AudioClip clip in enemyType.audioClips)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            FlashlightItem[] allFlashlightItems = Resources.FindObjectsOfTypeAll<FlashlightItem>();
            foreach (FlashlightItem flashlightItem in allFlashlightItems)
            {
                if (flashlightItem != null)
                {
                    DedupeAudio(flashlightItem.outOfBatteriesClip);
                    DedupeAudio(flashlightItem.flashlightFlicker);
                    foreach (AudioClip clip in flashlightItem.flashlightClips)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            NoisemakerProp[] allNoisemakerProps = Resources.FindObjectsOfTypeAll<NoisemakerProp>();
            foreach (NoisemakerProp noisemakerProp in allNoisemakerProps)
            {
                if (noisemakerProp != null)
                {
                    foreach (AudioClip clip in noisemakerProp.noiseSFX)
                    {
                        DedupeAudio(clip);
                    }
                    foreach (AudioClip clip in noisemakerProp.noiseSFXFar)
                    {
                        DedupeAudio(clip);
                    }
                }
            }

            foreach (AudioClip dupedAudio in dupedAudio)
            {
                GameObject.Destroy(dupedAudio);
                Resources.UnloadAsset(dupedAudio);
            }

            dupedAudio = [];
        }

        public static void DedupeAudio(AudioClip clip)
        {
            if (clip == null) return;

            if (AudioDict.TryGetValue(clip.name, out AudioClip processedAudio) && processedAudio.GetInstanceID() == clip.GetInstanceID())
            {
                // Already processed
            }
            else if (AudioDict.TryGetValue(clip.name, out AudioClip dedupedAudio))
            {
                dupedAudio.Add(clip);
                clip = dedupedAudio;
            }
            else
            {
                AddToAudioDict(clip.name, clip);
            }
        }

        public static void AddToAudioDict(string name, AudioClip audio)
        {
            if (name == "" || Config.deDupeTextureBlacklist.Value.ToLower().Trim().Split(';').Contains(name)) return;
            AudioDict.Add(name, audio);
        }
    }
}
