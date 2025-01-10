using HueHelper;
using Life;
using Life.BizSystem;
using Life.Network;
using Life.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using _menu = AAMenu.Menu;

namespace HuePlainte
{
    public class HuePlainte : ModKit.ModKit
    {
        public HuePlainte(IGameAPI aPI) : base(aPI) { }

        public string Name = Assembly.GetCallingAssembly().GetName().Name;
        public Config config;
        public class Config
        {
            public int LevelAdminRequiredToDeleteComplaint;
        }
        public void CreateConfig()
        {
            string directoryPath = pluginsPath + $"/{Name}";

            string configFilePath = directoryPath + "/config.json";

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(configFilePath))
            {
                var defaultConfig = new Config
                {
                    LevelAdminRequiredToDeleteComplaint = 4,
                };
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configFilePath, jsonContent);
            }

            config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        }
        public override void OnPluginInit()
        {
            base.OnPluginInit();
            ModKit.ORM.Orm.GetOrmInstance().RegisterTable<HuePlainteOrm>();
            AllHelper.InitHelper.InitMessage("V.1.0.0", "Zerox_Hue");
            CreateConfig();
            AddTabLineLawEnforcementPlainte();
            AddTabLineLawEnforcementControle();
            AddTabLineAllComplaint();
        }

        public void AddTabLineLawEnforcementControle()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, "Contrôler les plaintes", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                Control(player);
            });
        }
        public void AddTabLineLawEnforcementPlainte()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, "Déposer une plainte", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                Plainte(player);
            });
        }
        public void AddTabLineAllComplaint()
        {
            _menu.AddAdminTabLine(PluginInformations, 1, "Toutes les plaintes", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                OnClickAllComplaint(player);
            });
        }
        public async void OnClickAllComplaint(Player player)
        {
            var allelements = await HuePlainteOrm.Query(x => x.Résolu == false);

            UIPanel panel = new UIPanel("Toutes les plaintes", UIPanel.PanelType.Tab);

            AllHelper.PanelHelper.CloseButton(player, panel);

            if (!allelements.Any())
            {
                panel.AddTabLine("<color=#db0b35>Aucune plainte</color>", ui =>
                {
                    player.SendText($"<color=#c90e27>[HuePlainte]</color> Aucune plainte n'a été enregistré !");
                });
            }
            else
            {

                foreach (var elements in allelements)
                {
                    panel.AddTabLine($"<color=#2af537>{elements.Demandeur}</color>", ui =>
                    {
                        UIPanel panel1 = new UIPanel($"Plainte : <i><color=#1fd14f>{elements.Demandeur}</color></i>", UIPanel.PanelType.Text);

                        AllHelper.PanelHelper.CloseButton(player, panel1);

                        panel1.SetText($"Raison : <color=#15e826><i>{elements.Raison}</i></color> \n " +
                            $"Demandeur : <color=#15e826><i>{elements.Demandeur}</i></color>\n" +
                            $"Défendeur : <color=#15e826><i>{elements.Défendeur}</i></color> \n");

                        panel1.AddButton("Supprimer", async ui1 =>
                        {
                            if (player.account.adminLevel >= config.LevelAdminRequiredToDeleteComplaint)
                            {

                                player.Notify("Succés", "Vous avez supprimé la plainte avec succés !", NotificationManager.Type.Success);

                                player.ClosePanel(ui1);

                                elements.Demandeur = "Delete";
                                elements.Défendeur = "Delete";
                                elements.Raison = "Delete";
                                elements.Résolu = true;


                                await elements.Delete();
                                await elements.Save();
                            }
                            else
                            {
                                player.SendText($"<color=#c90e27>[HuePlainte]</color> Vous n'êtes pas administrateur Niveau <color=#1fd14f><b>{config.LevelAdminRequiredToDeleteComplaint.ToString()}</b></color> ou plus !");
                            }
                        });

                        AllHelper.PanelHelper.ShowPanel(player, panel1);
                    });
                }
            }

            AllHelper.PanelHelper.ValidButton(player, panel);

            AllHelper.PanelHelper.ShowPanel(player, panel);
        }
        public void Control(Player player)
        {
            UIPanel panel = new UIPanel("Contrôle De plainte", UIPanel.PanelType.Text);

            panel.SetText("JP : Joueur Proche ; NP : Nom et prénom d'un joueur");

            panel.AddButton("JP", ui =>
            {
                JpControl(player);
            });

            panel.AddButton("NP", ui =>
            {
                NpControl(player);
            });

            AllHelper.PanelHelper.ShowPanel(player, panel);

        }
        public void NpControl(Player player)
        {
            UIPanel panel = new UIPanel("Nom et prénom contrôle", UIPanel.PanelType.Input);

            AllHelper.PanelHelper.CloseButton(player, panel);

            panel.SetText("Nom et prénom :");

            panel.SetInputPlaceholder("Saisissez le nom et prénom...");

            panel.AddButton("Valider", ui =>
            {
                searchInData(player, panel.inputText);
            });

            AllHelper.PanelHelper.ShowPanel(player, panel);
        }
        public void JpControl(Player player)
        {
            var target = player.GetClosestPlayer();
            if (target != null)
            {
                if (target.setup.NetworkisRestrain)
                {
                    Nova.server.SendLocalText($"<color=#2af537>Le membre des forces de l'odre regarde les plaintes de {target.GetFullName()} !</color>", 5, player.setup.transform.position);

                    searchInData(player, target.GetFullName());
                }
                else
                {
                    UIPanel panel = new UIPanel("Demande", UIPanel.PanelType.Text);

                    panel.SetText("Un membre des forces de l'ordre vous demande vos plaintes");

                    panel.AddButton("Refuser", ui =>
                    {
                        Nova.server.SendLocalText("<color=#de1414>L'individu refuse de laisser le policier regarder ses plaintes !</color>", 5, target.setup.transform.position);

                        target.ClosePanel(ui);
                    });


                    panel.AddButton("Accepter", ui =>
                    {
                        Nova.server.SendLocalText($"<color=#2af537>{target.GetFullName()} accepte de laisser le policier regarder ses plaintes !</color>", 5, target.setup.transform.position);

                        searchInData(player, target.GetFullName());

                        target.ClosePanel(ui);
                    });

                    AllHelper.PanelHelper.ShowPanel(target, panel);
                }
            }
            else
            {
                player.SendText($"<color=#c90e27>[HuePlainte]</color> Aucun joueur à proximité !");
            }
        }
        public async void searchInData(Player player, string name)
        {
            var element = await HuePlainteOrm.Query(x => x.Défendeur == name);

            if (element.Any())
            {
                player.SendText($"<color=#c90e27>[HuePlainte]</color> Le joueur a au moins déja une plainte !");

                UIPanel panel = new UIPanel("Toutes les plaintes", UIPanel.PanelType.Tab);

                foreach (var elements in element)
                {
                    panel.AddTabLine(elements.Demandeur, ui =>
                    {
                        UIPanel panel1 = new UIPanel("Information Plainte", UIPanel.PanelType.Text);

                        panel1.SetText($"Raison : <color=#15e826><i>{elements.Raison}</i></color> \n " +
                            $"Demandeur : <color=#15e826><i>{elements.Demandeur}</i></color>\n" +
                            $"Défendeur : <color=#15e826><i>{elements.Défendeur}</i></color> \n");

                        AllHelper.PanelHelper.CloseButton(player, panel1);

                        panel1.AddButton("Supprimer", async ui1 =>
                        {
                            player.Notify("Succés", "Vous avez supprimé la plainte avec succés !", NotificationManager.Type.Success);

                            player.ClosePanel(ui1);

                            elements.Demandeur = "Delete";
                            elements.Défendeur = "Delete";
                            elements.Raison = "Delete";
                            elements.Résolu = true;
                            await elements.Delete();
                            await elements.Save();
                        });



                        AllHelper.PanelHelper.ShowPanel(player, panel1);
                    });
                }

                AllHelper.PanelHelper.CloseButton(player, panel);

                AllHelper.PanelHelper.ValidButton(player, panel);

                AllHelper.PanelHelper.ShowPanel(player, panel);
            }
            else
            {
                player.SendText("<color=#de1414>[HuePlainte]</color> Ce joueur n'a aucune plainte !");
            }
        }
        public void Plainte(Player player)
        {
            UIPanel panel = new UIPanel("Plainte Nom du demandeur", UIPanel.PanelType.Input);

            AllHelper.PanelHelper.CloseButton(player, panel);

            panel.SetText("Nom et prénom Du demandeur :");

            panel.SetInputPlaceholder("Saisissez le nom et prénom du demandeur...");

            panel.AddButton("Valider", ui =>
            {
                SecondPanel(player, panel);
            });

            AllHelper.PanelHelper.ShowPanel(player, panel);
        }
        public void SecondPanel(Player player, UIPanel panel1)
        {
            UIPanel panel = new UIPanel("Plainte Nom du défendeur", UIPanel.PanelType.Input);

            AllHelper.PanelHelper.CloseButton(player, panel);

            panel.SetText("Nom et prénom du défendeur :");

            panel.SetInputPlaceholder("Saisissez le nom et prénom du défendeur...");

            panel.AddButton("Valider", ui =>
            {
                ThirdPanel(player, panel1, panel);
            });

            AllHelper.PanelHelper.ShowPanel(player, panel);
        }
        public void ThirdPanel(Player player, UIPanel panel1, UIPanel panel2)
        {
            UIPanel panel = new UIPanel("Plainte Raison", UIPanel.PanelType.Input);

            AllHelper.PanelHelper.CloseButton(player, panel);

            panel.SetText("Raison :");

            panel.SetInputPlaceholder("Saissisez La raison de la plainte...");

            panel.AddButton("Valider", ui =>
            {
                CreateInstancePlainte(player, panel1, panel2, panel);
                player.ClosePanel(ui);
            });

            AllHelper.PanelHelper.ShowPanel(player, panel);

        }

        public async void CreateInstancePlainte(Player player, UIPanel panel, UIPanel panel1, UIPanel panel2)
        {
            HuePlainteOrm instance = new HuePlainteOrm();
            instance.Demandeur = panel.inputText;
            instance.Défendeur = panel1.inputText;
            instance.Raison = panel2.inputText;
            instance.Résolu = false;

            bool isSave = await instance.Save();
            if (isSave)
            {
                player.SendText($"<color=#c90e27>[HuePlainte]</color> La plainte a bien été enregistré !");
            }
            else
            {
                player.SendText($"<color=#c90e27>[HuePlainte]</color> Une erreur est survenue lors de l'enregistrement de la plainte merci de réessayer ultérirement si le probléme persiste merci d'en parler à un admin !");
            }
        }
    }
}
