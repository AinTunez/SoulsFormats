﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats
{
    public partial class MSB2
    {
        internal enum PartType : ushort
        {
            MapPiece = 0,
            Object = 1,
            Collision = 3,
            Navmesh = 4,
            ConnectCollision = 5,
        }

        /// <summary>
        /// Concrete map elements.
        /// </summary>
        public class PartsParam : Param<Part>, IMsbParam<IMsbPart>
        {
            internal override int Version => 5;
            internal override string Name => "PARTS_PARAM_ST";

            /// <summary>
            /// Visible but intangible models.
            /// </summary>
            public List<Part.MapPiece> MapPieces { get; set; }

            /// <summary>
            /// Dynamic or interactible elements.
            /// </summary>
            public List<Part.Object> Objects { get; set; }

            /// <summary>
            /// Invisible but physical surfaces.
            /// </summary>
            public List<Part.Collision> Collisions { get; set; }

            /// <summary>
            /// AI navigation meshes.
            /// </summary>
            public List<Part.Navmesh> Navmeshes { get; set; }

            /// <summary>
            /// Connections to other maps.
            /// </summary>
            public List<Part.ConnectCollision> ConnectCollisions { get; set; }

            /// <summary>
            /// Creates an empty PartsParam.
            /// </summary>
            public PartsParam()
            {
                MapPieces = new List<Part.MapPiece>();
                Objects = new List<Part.Object>();
                Collisions = new List<Part.Collision>();
                Navmeshes = new List<Part.Navmesh>();
                ConnectCollisions = new List<Part.ConnectCollision>();
            }

            /// <summary>
            /// Adds a part to the appropriate list for its type; returns the part.
            /// </summary>
            public Part Add(Part part)
            {
                switch (part)
                {
                    case Part.MapPiece p: MapPieces.Add(p); break;
                    case Part.Object p: Objects.Add(p); break;
                    case Part.Collision p: Collisions.Add(p); break;
                    case Part.Navmesh p: Navmeshes.Add(p); break;
                    case Part.ConnectCollision p: ConnectCollisions.Add(p); break;

                    default:
                        throw new ArgumentException($"Unrecognized type {part.GetType()}.", nameof(part));
                }
                return part;
            }
            IMsbPart IMsbParam<IMsbPart>.Add(IMsbPart item) => Add((Part)item);

            /// <summary>
            /// Returns every Part in the order they'll be written.
            /// </summary>
            public override List<Part> GetEntries()
            {
                return SFUtil.ConcatAll<Part>(
                    MapPieces, Objects, Collisions, Navmeshes, ConnectCollisions);
            }
            IReadOnlyList<IMsbPart> IMsbParam<IMsbPart>.GetEntries() => GetEntries();

            internal override Part ReadEntry(BinaryReaderEx br)
            {
                PartType type = br.GetEnum16<PartType>(br.Position + 8);
                switch (type)
                {
                    case PartType.MapPiece:
                        return MapPieces.EchoAdd(new Part.MapPiece(br));

                    case PartType.Object:
                        return Objects.EchoAdd(new Part.Object(br));

                    case PartType.Collision:
                        return Collisions.EchoAdd(new Part.Collision(br));

                    case PartType.Navmesh:
                        return Navmeshes.EchoAdd(new Part.Navmesh(br));

                    case PartType.ConnectCollision:
                        return ConnectCollisions.EchoAdd(new Part.ConnectCollision(br));

                    default:
                        throw new NotImplementedException($"Unimplemented part type: {type}");
                }
            }
        }

        /// <summary>
        /// A concrete map element.
        /// </summary>
        public abstract class Part : NamedEntry, IMsbPart
        {
            private protected abstract PartType Type { get; }

            /// <summary>
            /// The name of the part's model, referencing ModelParam.
            /// </summary>
            public string ModelName { get; set; }
            private int ModelIndex;

            /// <summary>
            /// Location of the part.
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Rotation of the part, in degrees.
            /// </summary>
            public Vector3 Rotation { get; set; }

            /// <summary>
            /// Scale of the part; only supported for map pieces and objects.
            /// </summary>
            public Vector3 Scale { get; set; }

            /// <summary>
            /// Not confirmed; determines when the part is loaded.
            /// </summary>
            public uint[] DrawGroups { get; private set; }

            /// <summary>
            /// Unknown; possibly nvm groups.
            /// </summary>
            public int Unk44 { get; set; }

            /// <summary>
            /// Unknown; possibly nvm groups.
            /// </summary>
            public int Unk48 { get; set; }

            /// <summary>
            /// Unknown; possibly nvm groups.
            /// </summary>
            public int Unk4C { get; set; }

            /// <summary>
            /// Unknown; possibly nvm groups.
            /// </summary>
            public int Unk50 { get; set; }

            /// <summary>
            /// Not confirmed; determines when the part is visible.
            /// </summary>
            public uint[] DispGroups { get; private set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk64 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk68 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk6C { get; set; }

            private protected Part(string name)
            {
                Name = name;
                Scale = Vector3.One;
                DrawGroups = new uint[4] {
                    0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
                DispGroups = new uint[4] {
                    0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };
            }

            private protected Part(BinaryReaderEx br)
            {
                long start = br.Position;
                long nameOffset = br.ReadInt64();
                br.AssertUInt16((ushort)Type);
                br.ReadInt16(); // Index
                ModelIndex = br.ReadInt32();
                Position = br.ReadVector3();
                Rotation = br.ReadVector3();
                Scale = br.ReadVector3();
                DrawGroups = br.ReadUInt32s(4);
                Unk44 = br.ReadInt32();
                Unk48 = br.ReadInt32();
                Unk4C = br.ReadInt32();
                Unk50 = br.ReadInt32();
                DispGroups = br.ReadUInt32s(4);
                Unk64 = br.ReadInt32();
                Unk68 = br.ReadInt32();
                Unk6C = br.ReadInt32();
                long typeDataOffset = br.ReadInt64();
                br.AssertInt64(0);

                Name = br.GetUTF16(start + nameOffset);

                br.Position = start + typeDataOffset;
                ReadTypeData(br);
            }

            internal abstract void ReadTypeData(BinaryReaderEx br);

            internal override void Write(BinaryWriterEx bw, int index)
            {
                long start = bw.Position;
                bw.ReserveInt64("NameOffset");
                bw.WriteUInt16((ushort)Type);
                bw.WriteInt16((short)index);
                bw.WriteInt32(ModelIndex);
                bw.WriteVector3(Position);
                bw.WriteVector3(Rotation);
                bw.WriteVector3(Scale);
                bw.WriteUInt32s(DrawGroups);
                bw.WriteInt32(Unk44);
                bw.WriteInt32(Unk48);
                bw.WriteInt32(Unk4C);
                bw.WriteInt32(Unk50);
                bw.WriteUInt32s(DispGroups);
                bw.WriteInt32(Unk64);
                bw.WriteInt32(Unk68);
                bw.WriteInt32(Unk6C);
                bw.ReserveInt64("TypeDataOffset");
                bw.WriteInt64(0);

                long nameStart = bw.Position;
                bw.FillInt64("NameOffset", nameStart - start);
                bw.WriteUTF16(MSB.ReambiguateName(Name), true);
                if (bw.Position - nameStart < 0x20)
                    bw.Position += 0x20 - (bw.Position - nameStart);
                bw.Pad(8);

                bw.FillInt64("TypeDataOffset", bw.Position - start);
                WriteTypeData(bw);
            }

            internal abstract void WriteTypeData(BinaryWriterEx bw);

            internal virtual void GetNames(MSB2 msb, Entries entries)
            {
                ModelName = MSB.FindName(entries.Models, ModelIndex);
            }

            internal virtual void GetIndices(Lookups lookups)
            {
                ModelIndex = FindIndex(lookups.Models, ModelName);
            }

            /// <summary>
            /// Returns a string representation of the part.
            /// </summary>
            public override string ToString()
            {
                return $"{Type} \"{Name}\"";
            }

            /// <summary>
            /// A visible but intangible model.
            /// </summary>
            public class MapPiece : Part
            {
                private protected override PartType Type => PartType.MapPiece;

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT00 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT02 { get; set; }

                /// <summary>
                /// Creates a MapPiece with default values.
                /// </summary>
                public MapPiece() : base("mXXXX_XXXX") { }

                internal MapPiece(BinaryReaderEx br) : base(br) { }

                internal override void ReadTypeData(BinaryReaderEx br)
                {
                    UnkT00 = br.ReadInt16();
                    UnkT02 = br.ReadInt16();
                    br.AssertInt32(0);
                }

                internal override void WriteTypeData(BinaryWriterEx bw)
                {
                    bw.WriteInt16(UnkT00);
                    bw.WriteInt16(UnkT02);
                    bw.WriteInt32(0);
                }
            }

            /// <summary>
            /// A dynamic or interactible element.
            /// </summary>
            public class Object : Part
            {
                private protected override PartType Type => PartType.Object;

                /// <summary>
                /// Unknown.
                /// </summary>
                public int MapObjectInstanceParamID { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT04 { get; set; }

                /// <summary>
                /// Creates an Object with default values.
                /// </summary>
                public Object() : base("oXX_XXXX_XXXX") { }

                internal Object(BinaryReaderEx br) : base(br) { }

                internal override void ReadTypeData(BinaryReaderEx br)
                {
                    MapObjectInstanceParamID = br.ReadInt32();
                    UnkT04 = br.ReadInt32();
                    br.AssertInt32(0);
                    br.AssertInt32(0);
                }

                internal override void WriteTypeData(BinaryWriterEx bw)
                {
                    bw.WriteInt32(MapObjectInstanceParamID);
                    bw.WriteInt32(UnkT04);
                    bw.WriteInt32(0);
                    bw.WriteInt32(0);
                }
            }

            /// <summary>
            /// An invisible but physical surface that controls map loading and graphics settings, among other things.
            /// </summary>
            public class Collision : Part
            {
                private protected override PartType Type => PartType.Collision;

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT00 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT04 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT08 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT0C { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT10 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public byte UnkT12 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public byte UnkT13 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT14 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT18 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT1C { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT20 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT24 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT26 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT28 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT2C { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT2E { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT30 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT34 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public short UnkT36 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT38 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT3C { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT40 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT44 { get; set; }

                /// <summary>
                /// Creates a Collision with default values.
                /// </summary>
                public Collision() : base("hXX_XXXX_XXXX") { }

                internal Collision(BinaryReaderEx br) : base(br) { }

                internal override void ReadTypeData(BinaryReaderEx br)
                {
                    UnkT00 = br.ReadInt32();
                    UnkT04 = br.ReadInt32();
                    UnkT08 = br.ReadInt32();
                    UnkT0C = br.ReadInt32();
                    UnkT10 = br.ReadInt16();
                    UnkT12 = br.ReadByte();
                    UnkT13 = br.ReadByte();
                    UnkT14 = br.ReadInt32();
                    UnkT18 = br.ReadInt32();
                    UnkT1C = br.ReadInt32();
                    UnkT20 = br.ReadInt32();
                    UnkT24 = br.ReadInt16();
                    UnkT26 = br.ReadInt16();
                    UnkT28 = br.ReadInt32();
                    UnkT2C = br.ReadInt16();
                    UnkT2E = br.ReadInt16();
                    UnkT30 = br.ReadInt32();
                    UnkT34 = br.ReadInt16();
                    UnkT36 = br.ReadInt16();
                    UnkT38 = br.ReadInt32();
                    UnkT3C = br.ReadInt32();
                    UnkT40 = br.ReadInt32();
                    UnkT44 = br.ReadInt32();
                    br.AssertPattern(0x10, 0x00);
                }

                internal override void WriteTypeData(BinaryWriterEx bw)
                {
                    bw.WriteInt32(UnkT00);
                    bw.WriteInt32(UnkT04);
                    bw.WriteInt32(UnkT08);
                    bw.WriteInt32(UnkT0C);
                    bw.WriteInt16(UnkT10);
                    bw.WriteByte(UnkT12);
                    bw.WriteByte(UnkT13);
                    bw.WriteInt32(UnkT14);
                    bw.WriteInt32(UnkT18);
                    bw.WriteInt32(UnkT1C);
                    bw.WriteInt32(UnkT20);
                    bw.WriteInt16(UnkT24);
                    bw.WriteInt16(UnkT26);
                    bw.WriteInt32(UnkT28);
                    bw.WriteInt16(UnkT2C);
                    bw.WriteInt16(UnkT2E);
                    bw.WriteInt32(UnkT30);
                    bw.WriteInt16(UnkT34);
                    bw.WriteInt16(UnkT36);
                    bw.WriteInt32(UnkT38);
                    bw.WriteInt32(UnkT3C);
                    bw.WriteInt32(UnkT40);
                    bw.WriteInt32(UnkT44);
                    bw.WritePattern(0x10, 0x00);
                }
            }

            /// <summary>
            /// An AI navigation mesh.
            /// </summary>
            public class Navmesh : Part
            {
                private protected override PartType Type => PartType.Navmesh;

                /// <summary>
                /// Unknown; possibly nvm groups.
                /// </summary>
                public int UnkT00 { get; set; }

                /// <summary>
                /// Unknown; possibly nvm groups.
                /// </summary>
                public int UnkT04 { get; set; }

                /// <summary>
                /// Unknown; possibly nvm groups.
                /// </summary>
                public int UnkT08 { get; set; }

                /// <summary>
                /// Unknown; possibly nvm groups.
                /// </summary>
                public int UnkT0C { get; set; }

                /// <summary>
                /// Creates a Navmesh with default values.
                /// </summary>
                public Navmesh() : base("nXX_XXXX_XXXX") { }

                internal Navmesh(BinaryReaderEx br) : base(br) { }

                internal override void ReadTypeData(BinaryReaderEx br)
                {
                    UnkT00 = br.ReadInt32();
                    UnkT04 = br.ReadInt32();
                    UnkT08 = br.ReadInt32();
                    UnkT0C = br.ReadInt32();
                    br.AssertPattern(0x10, 0x00);
                }

                internal override void WriteTypeData(BinaryWriterEx bw)
                {
                    bw.WriteInt32(UnkT00);
                    bw.WriteInt32(UnkT04);
                    bw.WriteInt32(UnkT08);
                    bw.WriteInt32(UnkT0C);
                    bw.WritePattern(0x10, 0x00);
                }
            }

            /// <summary>
            /// Causes another map to be loaded when standing on the referenced collision.
            /// </summary>
            public class ConnectCollision : Part
            {
                private protected override PartType Type => PartType.ConnectCollision;

                /// <summary>
                /// Name of the referenced collision part.
                /// </summary>
                public string CollisionName { get; set; }
                private int CollisionIndex;

                /// <summary>
                /// The map to load when on this collision.
                /// </summary>
                public byte[] MapID { get; private set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT08 { get; set; }

                /// <summary>
                /// Unknown.
                /// </summary>
                public int UnkT0C { get; set; }

                /// <summary>
                /// Creates a ConnectCollision with default values.
                /// </summary>
                public ConnectCollision() : base("hXX_XXXX_XXXX")
                {
                    MapID = new byte[4];
                }

                internal ConnectCollision(BinaryReaderEx br) : base(br) { }

                internal override void ReadTypeData(BinaryReaderEx br)
                {
                    CollisionIndex = br.ReadInt32();
                    MapID = br.ReadBytes(4);
                    UnkT08 = br.ReadInt32();
                    UnkT0C = br.ReadInt32();
                }

                internal override void WriteTypeData(BinaryWriterEx bw)
                {
                    bw.WriteInt32(CollisionIndex);
                    bw.WriteBytes(MapID);
                    bw.WriteInt32(UnkT08);
                    bw.WriteInt32(UnkT0C);
                }

                internal override void GetNames(MSB2 msb, Entries entries)
                {
                    base.GetNames(msb, entries);
                    CollisionName = MSB.FindName(msb.Parts.Collisions, CollisionIndex);
                }

                internal override void GetIndices(Lookups lookups)
                {
                    base.GetIndices(lookups);
                    CollisionIndex = FindIndex(lookups.Collisions, CollisionName);
                }
            }
        }
    }
}
