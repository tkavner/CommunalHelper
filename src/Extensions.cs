﻿using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper {
    public static class Extensions {

        private static DynData<Player> cachedPlayerData;

        public static DynData<Player> GetData(this Player player) {
            if (cachedPlayerData != null && cachedPlayerData.IsAlive && cachedPlayerData.Target == player)
                return cachedPlayerData;
            return cachedPlayerData = new DynData<Player>(player);
        }

        public static Color Mult(this Color color, Color other) {
            color.R = (byte) (color.R * other.R / 256f);
            color.G = (byte) (color.G * other.G / 256f);
            color.B = (byte) (color.B * other.B / 256f);
            color.A = (byte) (color.A * other.A / 256f);
            return color;
        }

        public static Vector2 CorrectJoystickPrecision(this Vector2 dir) {
            if (dir.X != 0f && Math.Abs(dir.X) < 0.001f) {
                dir.X = 0f;
                dir.Y = Math.Sign(dir.Y);
            } else if (dir.Y != 0f && Math.Abs(dir.Y) < 0.001f) {
                dir.Y = 0f;
                dir.X = Math.Sign(dir.X);
            }
            return dir;
        }

        // Dream Tunnel Dash related extension methods located in DreamTunnelDash.cs

        #region Collider

        public static T CollideFirst<T, Exclude>(this Entity from) where T : Entity where Exclude : Entity {
            List<Entity> list = from.Scene.Tracker.Entities[typeof(Exclude)];
            foreach (Entity entity in from.Scene.Tracker.Entities[typeof(T)]) {
                if (!list.Contains(entity) && Collide.Check(from, entity)) {
                    return entity as T;
                }
            }
            return null;
        }

        public static T CollideFirst<T, Exclude>(this Entity from, Vector2 at) where T : Entity where Exclude : Entity {
            Vector2 position = from.Position;
            from.Position = at;
            T result = CollideFirst<T, Exclude>(from);
            from.Position = position;
            return result;
        }

        public static bool Contains(this Collider container, Collider contained, float padding = 0) {
            if (container.AbsoluteLeft - padding < contained.AbsoluteLeft && container.AbsoluteTop - padding < contained.AbsoluteTop &&
                container.AbsoluteRight + padding > contained.AbsoluteRight && container.AbsoluteBottom + padding > contained.AbsoluteBottom)
                return true;
            return false;
        }

        #endregion

        #region WallBoosters

        public static bool AttachedWallBoosterCheck(this Player player) {
            foreach (AttachedWallBooster wallbooster in player.Scene.Tracker.GetEntities<AttachedWallBooster>()) {
                if (player.Facing == wallbooster.Facing && player.CollideCheck(wallbooster))
                    return true;
            }
            return false;
        }

        #endregion

        public static void PointBounce(this Player player, Vector2 from, float force) {
            if (player.StateMachine.State == 2) {
                player.StateMachine.State = 0;
            }
            if (player.StateMachine.State == 4 && player.CurrentBooster != null) {
                player.CurrentBooster.PlayerReleased();
            }
            player.RefillDash();
            player.RefillStamina();
            Vector2 vector = (player.Center - from).SafeNormalize();
            if (vector.Y > -0.2f && vector.Y <= 0.4f) {
                vector.Y = -0.2f;
            }
            player.Speed = vector * force;
            player.Speed.X *= 1.5f;
            if (Math.Abs(player.Speed.X) < 100f) {
                if (player.Speed.X == 0f) {
                    player.Speed.X = -(float) player.Facing * 100f;
                    return;
                }
                player.Speed.X = Math.Sign(player.Speed.X) * 100f;
            }
        }

        public static void PointBounce(this Holdable holdable, Vector2 from) {
            Vector2 vector = (holdable.Entity.Center - from).SafeNormalize();
            if (vector.Y > -0.2f && vector.Y <= 0.4f) {
                vector.Y = -0.2f;
            }
            holdable.Release(vector);
        }


        #region JaThePlayer's state machine extension code

        /// <summary>
        /// Adds a state to a StateMachine
        /// </summary>
        /// <returns>The index of the new state</returns>
        public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
            Action[] begins = (Action[]) StateMachine_begins.GetValue(machine);
            Func<int>[] updates = (Func<int>[]) StateMachine_updates.GetValue(machine);
            Action[] ends = (Action[]) StateMachine_ends.GetValue(machine);
            Func<IEnumerator>[] coroutines = (Func<IEnumerator>[]) StateMachine_coroutines.GetValue(machine);
            int nextIndex = begins.Length;
            // Now let's expand the arrays
            Array.Resize(ref begins, begins.Length + 1);
            Array.Resize(ref updates, begins.Length + 1);
            Array.Resize(ref ends, begins.Length + 1);
            Array.Resize(ref coroutines, coroutines.Length + 1);
            // Store the resized arrays back into the machine
            StateMachine_begins.SetValue(machine, begins);
            StateMachine_updates.SetValue(machine, updates);
            StateMachine_ends.SetValue(machine, ends);
            StateMachine_coroutines.SetValue(machine, coroutines);
            // And now we add the new functions
            machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
            return nextIndex;
        }
        private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);

        #endregion

    }
}
