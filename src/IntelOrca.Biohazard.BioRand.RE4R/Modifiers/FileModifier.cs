using System;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class FileModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileService = randomizer.FileService;
            PlaceWelcomeDocument();
            foreach (var placement in fileService.FilePlacements)
            {
                PlaceFileGimmick(placement);
                BuildFile(placement);
            }

            void PlaceWelcomeDocument()
            {
                fileService.FilePlacements.Add(new FilePlacement()
                {
                    TemplateId = 32,
                    Id = 1,
                    Stage = 40502,
                    X = -179.69f,
                    Y = 10.64f,
                    Z = 58.31f,
                    Yaw = 45,
                    Content =
                        """
                        <COLOR FFFF00>Welcome to BioRand 2.0</COLOR>
                        <PAGE>
                        Dear <COL FILE>player</COL>,

                        Welcome to the 2.0 version of the Resident Evil 4 remake Randomizer. I’m thrilled to see how popular this randomizer has become, and I’m deeply grateful for the incredible support I have received for it.

                        With this new version, there are lots of new exciting features to keep you on edge and surprise you with each seed.
                        <PAGE>
                        <COLOR 00FFFF>Random events</COLOR>
                        Each seed will have a number of events that occur. You may occasionally find yourself locked in a room or area forcing you to fight foes until the doors unlock.

                        Keys will not always be where they normally are. Some will be held by a tough enemy nearby, others will be completely relocated. Keep your eyes open for files that provide clues.
                        <PAGE>
                        <COLOR 00FFFF>Rare legendary weapons</COLOR>
                        
                        Hidden within certain seeds are powerful, one-of-a-kind weapons. These weapons contain powerful memes and unique mechanics.

                        Gun rhymes with fun for a reason.
                        <PAGE>
                        This project wouldn’t have been possible without the help of some amazing contributors:

                        - <COL FILE>7rayD</COL>
                        - <COL FILE>MightKusKus</COL>
                        - <COL FILE>Afkkun</COL>
                        - <COL FILE>Shinypockets</COL>
                        - <COL FILE>404runnotfound</COL>
                        <PAGE>
                        I hope this randomizer continues to surprise, challenge, and entertain you for many runs to come.

                        Thank you for playing!
                        IntelOrca
                        """
                });
            }

            void PlaceFileGimmick(FilePlacement placement)
            {
                var contextId = randomizer.FlagService.AllocateContextId(1, 1);
                var transform = new Transform
                {
                    Position = new Vector3(placement.X, placement.Y, placement.Z),
                    Eular = new EulerAngles(placement.Yaw, placement.Pitch, placement.Roll),
                    Scale = Vector3.One
                };

                var gimmick = GimmickTemplate.Get("Biorand_Document").Clone();
                gimmick = gimmick.AddOrUpdateComponent(transform.ToComponent());
                gimmick = gimmick.WithGimmickContextId(contextId);

                var gmReadFile = gimmick.FindComponent("chainsaw.GmReadFile")!;
                gmReadFile = gmReadFile.Set("_Userdata", new RszUserDataNode(
                    randomizer.FileRepository.TypeRepository.FromName("chainsaw.DetailSearchFileUserdata")!,
                    $"_Chainsaw/LevelDesign/Prefab/ReadFile/File_{placement.Id:000}/File_{placement.Id:000}_00_FileUserdata.user"));
                gimmick = gimmick.AddOrUpdateComponent(gmReadFile);

                var area = randomizer.AreaService.FindBestArea(AreaKind.Gimmicks, placement.Stage);
                area.Scene = area.Scene.Add(gimmick);
                area.GimmickSaveData.Add(new GimmickSaveDataTable.Data()
                {
                    ID = contextId,
                    Save = new GimmickContext.SaveData()
                    {
                        Attr = [0, 0, 0, 0]
                    },
                    Static = new chainsaw.GmContextReadFile.StaticDataReadFile()
                    {
                        Position = transform.Position,
                        DocID = placement.Id,
                        Stage = placement.Stage
                    },
                    ContextType = "chainsaw.GmContextReadFile"
                });
            }

            void BuildFile(FilePlacement placement)
            {
                // Replace text
                var msgPath = $"natives/stm/_chainsaw/message/mes_main_file/ch_mes_main_file_{placement.Id:000}.msg.22";
                var msgFile = randomizer.FileRepository.GetMsgFile(msgPath).ToBuilder();
                var titleGuid = SetContent(msgFile, placement.Id, placement.Content, out var numPages);
                randomizer.FileRepository.SetMsgFile(msgPath, msgFile.Build());

                // Update metadata
                randomizer.FileRepository.ModifyUserFile<chainsaw.FileSettingUserdata>("natives/stm/_chainsaw/appsystem/ui/userdata/filesettinguserdata.user.2", root =>
                {
                    var template = root._Datas[placement.TemplateId];

                    var index = root._Datas.FindIndex(x => x._FileID == placement.Id);
                    root._Datas[index] = new FileSettingUserdata.Data()
                    {
                        _Enable = true,
                        _FileID = placement.Id,
                        _LocationType = 1,
                        _MsgID = titleGuid,
                        _EachPage = Enumerable.Range(0, numPages).Select(x => new FileSettingUserdata.EachPage() { _BackTextureID = template._EachPage[0]._BackTextureID }).ToList()
                    };
                    return root;
                });

                // DetailSerarchFileUserdata
                {
                    var sourcePath = GetDetailSearchFileUserdataPath(placement.TemplateId);
                    var targetPath = GetDetailSearchFileUserdataPath(placement.Id);
                    var userDataFile = randomizer.FileRepository.GetUserFile(sourcePath).ToBuilder(randomizer.FileRepository.TypeRepository);
                    var userData = RszSerializer.Deserialize<chainsaw.DetailSearchFileUserdata>(userDataFile.Objects[0])!;
                    userData._ID = placement.Id;
                    userDataFile.Objects =
                    [
                        randomizer.FileRepository.TypeRepository.Serialize(userData)
                    ];
                    randomizer.FileRepository.SetUserFile(targetPath, userDataFile.Build());
                }
            }

            static Guid SetContent(MsgFile.Builder file, int id, string content, out int numPages)
            {
                var titleGuid = file.FindMessage($"CH_Mes_Main_File_{id:000}_00")?.Guid ?? Guid.NewGuid();
                file.Attributes.Clear();
                file.Messages.Clear();

                var pages = content.Trim().Split("<PAGE>").Append("<END>").ToArray();
                for (var i = 0; i < pages.Length; i++)
                {
                    var name = $"CH_Mes_Main_File_{id:000}_{i:00}";
                    var guid = i == 0 ? titleGuid : name.GetGuidHash();
                    file.Create(guid, name, pages[i].Trim());
                }
                numPages = pages.Length - 1;
                return titleGuid;
            }

            static string GetDetailSearchFileUserdataPath(int id) => $"natives/stm/_chainsaw/leveldesign/prefab/readfile/file_{id:000}/file_{id:000}_00_fileuserdata.user.2";
        }
    }
}
