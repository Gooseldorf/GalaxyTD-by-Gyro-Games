using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable NotAccessedField.Local
namespace TestingAgent.Editor
{
    public sealed partial class TestingAgent
    {
        private const string ROOT_PATH = "Assets/Plugins/TestingAgent";
        private const string TEST_RESULTS_PATH = ROOT_PATH + "/Data";

        private enum TestCondition
        {
            MaxAttempts,
            ModifierSliceLessThan,
            CreepHpLessThanOrEqual,
            LoseWinAttempts
        }
        
        [Title("Debug")]
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private int attempts = 1;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private int resultCash;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private float currentHpModifier;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private float hpModifierStepSlice;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private int winCount;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private int loseCount;
        [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private int currentTestingDirective;
        
        // [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private string testTime = "0:00:00";
        // [ShowInInspector, ReadOnly, Indent, HideIf("@UnityEngine.Application.isPlaying==false")] private string currentIterationTime = "0:00:00";
        private Stopwatch testWatch = new();
        private Stopwatch iterationWatch = new();
        
        
        [Title("Primary settings")]
        [SerializeField, Indent] private int minCash = 100;
        [SerializeField, Indent] private int maxCash = 10_000;
        [SerializeField, Indent] private int extraCashForReload = 50;

        [Space]
        [SerializeField, Indent, LabelText("HP Modifier (Default)")] 
        private float defaultHpModifier = 1;
        [SerializeField, Indent(2), LabelText("Step")] 
        private float hpModifierStep = .5f;
        
        [Title("Test Condition")]
        [SerializeField, Indent, LabelText("Condition (To Pass Test)")] 
        private TestCondition condition = TestCondition.ModifierSliceLessThan;
        [SerializeField, Indent] 
        private int maxWinWaves = 100;

        [SerializeField, Indent(2), HideIf("@condition==TestCondition.MaxAttempts")] 
        private float minAvailableSlice = .03f;
        
        [SerializeField, Indent(2), HideIf("@condition!=TestCondition.MaxAttempts")] 
        private int maxAttempts = 100;
        
        [SerializeField, Indent(2), HideIf("@condition!=TestCondition.LoseWinAttempts")] 
        private int loseWinAttempts = 3;        
        [SerializeField, Indent(2), HideIf("@condition!=TestCondition.LoseWinAttempts")] 
        private float multiplierPerWin = 2;        
        [SerializeField, Indent(2), HideIf("@condition!=TestCondition.LoseWinAttempts")] 
        private float multiplierPerLose = .75f;
        
        [Title("Time")]
        [SerializeField, Indent] private float pushTimeValue = 10f;
        [Title("Time")]
        [SerializeField, Indent] private float timeScale = 10f;
        
        [Title("Visual")]
        [SerializeField, Indent] private bool enableVisual = true;

        [Title("Waves")]
        [SerializeField] private WaveData waves = new();

        [Title("Allowed to build towers")]
        [SerializeField, Indent] private int maxTowerLvl = 5;
        [SerializeField, Indent] private bool randomTowers;
        [HideIf("@randomTowers")]
        [SerializeField, Indent] private AllEnums.TowerId allowedTowers = new();

        [Title("Test directives")]
        [SerializeField, Indent] private bool testDirectivesOnly;
        [SerializeField, Indent] private DirectiveTestSet directiveProfile;


        private readonly List<TowerInfo> builtTowers = new();
        private readonly Stopwatch timer = new();
        private Mission mission;
        private GameData gameData;
        private TestResult currentResult;
        private int currentTestIteration;
        private int maxTestIteration;
        private int availableCash;
        private bool initialized;
        private bool isBusy;
        private bool? lastAttemptWinResult;
        private int loseWinAttemptsTemp;
        
        private bool rebuildWithNewDirective;
        private List<TowerFactory> towerFactories;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    }
}