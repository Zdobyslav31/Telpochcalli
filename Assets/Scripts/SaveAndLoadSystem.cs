using CodeMonkey;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveAndLoadSystem
{

    public void Save() {
        List<TileSaveObject> saveObjectList = new List<TileSaveObject>();
        for (int x = 0; x < GameHandler.Instance.GetMapWidth(); x++) {
            for (int y = 0; y < GameHandler.Instance.GetMapHeight(); y++) {
                GameObject warrior = GameHandler.Instance.GetCombatGrid().GetGridObject(x, y).GetWarrior();
                TileSaveObject saveObjectNode = new TileSaveObject {
                    x = x,
                    y = y,
                    terrainType = GameHandler.Instance.GetTerrainTilemap().GetTerrainType(x, y),
                    warrior = warrior ? new WarriorSaveObject {
                        unit = warrior.name,
                        team = warrior.GetComponent<WarriorUISystem>().team,
                        direction = warrior.GetComponent<WarriorUISystem>().direction
                    } : null
                };
                saveObjectList.Add(saveObjectNode);
            }
        }
        SaveObject saveObject = new SaveObject {
            saveObjectArray = saveObjectList.ToArray()
        };
        string saveString = JsonUtility.ToJson(saveObject);
        File.WriteAllText(Application.dataPath + "/Maps/save.txt", saveString);
        CMDebug.TextPopupMouse("Saved!");
    }

    public void Load() {
        if (File.Exists(Application.dataPath + "/Maps/save.txt")) {
            string saveString = File.ReadAllText(Application.dataPath + "/Maps/save.txt");
            CMDebug.TextPopupMouse("Loaded!");
            SaveObject saveObject = JsonUtility.FromJson<SaveObject>(saveString);
            TerrainMap terrainMap = GameHandler.Instance.GetTerrainTilemap();

            foreach (TileSaveObject tileSaveObject in saveObject.saveObjectArray) {
                terrainMap.SetTerrainType(
                    tileSaveObject.x,
                    tileSaveObject.y,
                    tileSaveObject.terrainType
                );
            }
        }
    }

    [System.Serializable]
    private class SaveObject {
        public TileSaveObject[] saveObjectArray;
    }

    [System.Serializable]
    private class TileSaveObject {
        public int x;
        public int y;
        public TerrainNode.TerrainType terrainType;
        public WarriorSaveObject warrior;
    }

    [System.Serializable]
    private class WarriorSaveObject {
        public string unit;
        public WarriorUISystem.Team team;
        public WarriorUISystem.Direction direction;
    }
}
