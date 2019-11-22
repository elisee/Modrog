﻿using SwarmBasics.Math;

namespace ModrogServer.Game
{
    sealed class InternalCharacterKind : ModrogApi.Server.CharacterKind
    {
        public readonly int Id;
        public readonly Point SpriteLocation;

        internal InternalCharacterKind(int id, Point spriteLocation)
        {
            Id = id;
            SpriteLocation = spriteLocation;
        }

        #region API
        #endregion
    }
}