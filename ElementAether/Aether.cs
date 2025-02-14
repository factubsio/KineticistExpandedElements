﻿using JetBrains.Annotations;
using KineticistElementsExpanded.Components;
using KineticistElementsExpanded.Components.Properties;
using KineticistElementsExpanded.KineticLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.ResourceLinks;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kingmaker.UnitLogic.FactLogic.AddMechanicsFeature;
using static Kingmaker.UnitLogic.Mechanics.Properties.BlueprintUnitProperty;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;

namespace KineticistElementsExpanded.ElementAether
{
    class Aether : Statics
    {

        private static BlueprintBuff ForceBladeBuff = null;
        private static BlueprintBuff TeleBladeBuff = null;

        public static void Configure()
        {
            var blast_progression = CreateFullTelekineticBlast(out var blast_feature, out var tb_blade_feature, out var tb_blast_ability);
            var force_blast_feature = CreateAetherCompositeBlasts(out var force_blade_feature, out var force_blast_ability, out var lesserAethericBoost, out var greaterAethericBoost, out var lesserAethericBuff, out var greaterAethericBuff);
            limitAethericBoosts(lesserAethericBuff, greaterAethericBuff, new BlueprintAbilityReference[] { tb_blast_ability.ToRef() }, new BlueprintAbilityReference[] { force_blast_ability.ToRef() });
            var aether_class_skills = CreateAetherClassSkills();
            var force_ward_feature = CreateForceWard(blast_feature);
            AddElementalDefenseIsPrereqFor(blast_feature, tb_blade_feature, force_ward_feature);
            var first_progression_aether = CreateAetherElementalFocus(blast_progression, aether_class_skills, force_ward_feature, lesserAethericBoost, greaterAethericBoost);
            var kinetic_knight_progression_aether = CreateKineticKnightAetherFocus(blast_progression, aether_class_skills, force_ward_feature);
            var second_progression_aether = CreateSecondElementAether(blast_progression, kinetic_knight_progression_aether, blast_feature, force_blast_feature);
            var third_progression_aether = CreateThirdElementAether(blast_progression, kinetic_knight_progression_aether, blast_feature, force_blast_feature, second_progression_aether);
            CreateAetherWildTalents(
                first_progression_aether, kinetic_knight_progression_aether, second_progression_aether, third_progression_aether, blast_feature,
                force_ward_feature);

            var whirl = Kineticist.blade_whirlwind.GetComponent<AbilityCasterHasFacts>();
            Helper.AppendAndReplace(ref whirl.m_Facts, ForceBladeBuff.ToRef2(), TeleBladeBuff.ToRef2());
        }

        private static BlueprintFeatureBase CreateAetherClassSkills()
        {
            var feature = Helper.CreateBlueprintFeature("AetherClassSkills", "Aether Class Skills",
                AetherClassSkillsDescription, null, null, 0)
                .SetComponents(
                Helper.CreateAddClassSkill(StatType.SkillThievery),
                Helper.CreateAddClassSkill(StatType.SkillKnowledgeWorld)
                );

            return feature;
        }

        private static void AddBlastsToMetakinesis(BlueprintAbility blast)
        {
            BlueprintBuff[] Metakinesis_buff_list = new BlueprintBuff[] {
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f5f3aa17dd579ff49879923fb7bc2adb"), // MetakinesisEmpowerBuff
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f690edc756b748e43bba232e0eabd004"), // MetakinesisQuickenBuff
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("870d7e67e97a68f439155bdf465ea191"), // MetakinesisMaximizedBuff
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f8d0f7099e73c95499830ec0a93e2eeb"), // MetakinesisEmpowerCheaperBuff
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("c4b74e4448b81d04f9df89ed14c38a95"), // MetakinesisQuickenCheaperBuff
                ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("b8f43f0040155c74abd1bc794dbec320") // MetakinesisMaximizedCheaperBuff
            };
            foreach (var metakinesis_buff in Metakinesis_buff_list)
            {
                AddKineticistBurnModifier component = metakinesis_buff.GetComponent<AddKineticistBurnModifier>();
                Helper.AppendAndReplace(ref component.m_AppliableTo, blast.ToRef());
                AutoMetamagic auto = metakinesis_buff.GetComponent<AutoMetamagic>();
                auto.Abilities.Add(blast.ToRef());
            }
        }

        private static void AddBlastsToBurn(BlueprintAbility blast)
        {
            BlueprintFeature[] BurnFeatureList =
            {
                ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("57e3577a0eb53294e9d7cc649d5239a3"), // BurnFeature
                ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("2fa48527ba627254ba9bf4556330a4d4"), // PsychokineticistBurnFeature
                ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("a3051f965d971ed44b9c6c63bf240b79"), // OverwhelmingSoulBurnFeature
                ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("42c5a9a8661db2f47aedf87fb8b27aaf")  // DarkElementalistBurnFeature
            };

            foreach (var burnFeature in BurnFeatureList)
            {
                var addKineticistPart = burnFeature.GetComponent<AddKineticistPart>();
                Helper.AppendAndReplace(ref addKineticistPart.m_Blasts, blast.ToRef());
            }
        }

        #region Elemental Focus Selection

        private static BlueprintProgression CreateAetherElementalFocus(BlueprintFeatureBase blast_progression, BlueprintFeatureBase aether_class_skills, BlueprintFeatureBase force_ward_feature, BlueprintFeatureBase lesserAethericBoost, BlueprintFeatureBase greaterAethericBoost)
        {
            var element_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("1f3a15a3-ae8a-5524-ab8b-97f469bf4e3d"); // First Kineticist Element Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var blood_kineticist_arch = Helper.ToRef<BlueprintArchetypeReference>("365b50db-a54e-fb74-fa24-c07e9b7a838c"); // Kineticist Archetype: Blood

            var progression = Helper.CreateBlueprintProgression("ElementalFocusAether", "Aether",
                ElementalFocusAetherDescription, ElementalFocusAetherGuid, null,
                FeatureGroup.KineticElementalFocus)
                .SetComponents(Helper.CreatePrerequisiteNoArchetype(blood_kineticist_arch, kineticist_class));

            var entry1 = Helper.CreateLevelEntry(1, blast_progression, aether_class_skills);
            var entry2 = Helper.CreateLevelEntry(2, force_ward_feature);
            //var entry3 = Helper.CreateLevelEntry(7, lesserAethericBoost);
            //var entry4 = Helper.CreateLevelEntry(15, greaterAethericBoost);
            //Helper.AddEntries(progression, entry1, entry2, entry3, entry4);
            Helper.AddEntries(progression, entry1, entry2);

            Helper.AppendAndReplace(ref element_selection.m_AllFeatures, progression.ToRef());
            return progression;
        }

        private static BlueprintProgression CreateKineticKnightAetherFocus(BlueprintFeatureBase blast_progression, BlueprintFeatureBase aether_class_skills, BlueprintFeatureBase force_ward_feature)
        {
            var element_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("b1f296f0-bd16-bc24-2ae3-5d0638df82eb"); // First Kineticist Element Selection - Kinetic Knight

            var progression = Helper.CreateBlueprintProgression("KineticKnightElementalFocusAether", "Aether",
                ElementalFocusAetherDescription, null, null,
                FeatureGroup.KineticElementalFocus);

            var entry1 = Helper.CreateLevelEntry(1, blast_progression, aether_class_skills);
            var entry2 = Helper.CreateLevelEntry(4, force_ward_feature);
            Helper.AddEntries(progression, entry1, entry2);

            Helper.AppendAndReplace(ref element_selection.m_AllFeatures, progression.ToRef());
            return progression;
        }

        private static BlueprintProgression CreateSecondElementAether(BlueprintFeatureBase blast_progression, BlueprintProgression knight_progression, BlueprintFeature blast_feature, BlueprintFeature force_blast_feature) {
            var element_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("4204bc10-b3d5-db44-0b1f-52f0c375848b"); // Second Kineticist Element Selection
            var composite_blast_buff = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>("cb30a291-c75d-ef84-0904-30fbf2b5c05e"); // Kineticist CompositeBlastBuff

            var progression = Helper.CreateBlueprintProgression("SecondaryElementAether", "Aether",
                ElementalFocusAetherDescription, null, null,
                FeatureGroup.KineticElementalFocus);
            progression.HideInCharacterSheetAndLevelUp = true;
            progression.SetComponents
                (
                Helper.CreateActivateTrigger
                    (
                    Helper.CreateConditionsChecker(Operation.Or, Helper.CreateHasFact(new FactOwner(), blast_progression.ToRef2()), Helper.CreateHasFact(new FactOwner(), knight_progression.ToRef2())),
                    Helper.CreateActionList(Helper.CreateAddFact(new FactOwner(), force_blast_feature.ToRef2()))
                    ),
                Helper.CreateAddFacts(composite_blast_buff.ToRef()),
                Helper.CreateAddFeatureIfHasFact(blast_feature.ToRef2())
                );

            progression.m_Classes = new BlueprintProgression.ClassWithLevel
            {
                AdditionalLevel = 0,
                m_Class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391")
            }.ObjToArray();

            var entry1 = Helper.CreateLevelEntry(7, blast_progression);
            Helper.AddEntries(progression, entry1);

            Helper.AppendAndReplace(ref element_selection.m_AllFeatures, progression.ToRef());
            return progression;
        }

        private static BlueprintProgression CreateThirdElementAether(BlueprintFeatureBase blast_progression, BlueprintProgression knight_progression, BlueprintFeature blast_feature, BlueprintFeature force_blast_feature, BlueprintProgression second_aether) {
            var element_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("e2c17188-28fc-8434-79f1-8ab4d75ded86"); // Third Kineticist Element Selection
            var composite_blast_buff = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>("cb30a291-c75d-ef84-0904-30fbf2b5c05e"); // Kineticist CompositeBlastBuff
            var progression = Helper.CreateBlueprintProgression("ThirdElementAether", "Aether",
                ElementalFocusAetherDescription, null, null,
                FeatureGroup.KineticElementalFocus);
            progression.HideInCharacterSheetAndLevelUp = true;
            progression.SetComponents
                (
                Helper.CreateActivateTrigger
                    (
                    Helper.CreateConditionsChecker(Operation.Or, Helper.CreateHasFact(new FactOwner(), blast_progression.ToRef2()), Helper.CreateHasFact(new FactOwner(), knight_progression.ToRef2())),
                    Helper.CreateActionList(Helper.CreateAddFact(new FactOwner(), force_blast_feature.ToRef2()))
                    ),
                Helper.CreateAddFacts(composite_blast_buff.ToRef()),
                Helper.CreatePrerequisiteNoFeature(second_aether.ToRef()),
                Helper.CreateAddFeatureIfHasFact(blast_feature.ToRef2())
                );

            progression.m_Classes = new BlueprintProgression.ClassWithLevel
            {
                AdditionalLevel = 0,
                m_Class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391")
            }.ObjToArray();

            var entry1 = Helper.CreateLevelEntry(15, blast_progression);
            Helper.AddEntries(progression, entry1);

            Helper.AppendAndReplace(ref element_selection.m_AllFeatures, progression.ToRef());
            return progression;
        }

        #endregion

        #region Force Ward

        private static BlueprintFeature Temp(BlueprintFeature tb_feature)
        {
            var icon = Helper.CreateSprite("forceWard.png");
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391");

            #region Effect Feature
            var fw_effect_feature = Helper.CreateBlueprintFeature("ForceWardEffectFeature", "Force Ward",
                ForceWardDescription, null, null, 0);
            fw_effect_feature.Ranks = 20;
            fw_effect_feature.ReapplyOnLevelUp = true;

            var feature_value_getter = new FeatureRankPlusBonusGetter()
            {
                Feature = fw_effect_feature.ToRef(),
                bonus = 3
            };
            var classlvl_value_getter = new ClassLevelGetter()
            {
                ClassRef = kineticist_class
            };
            var temp_hp_progression = Helper.CreateBlueprintUnitProperty("ForceWardHPProperty")
                .SetComponents
                (
                feature_value_getter,
                classlvl_value_getter
                );
            temp_hp_progression.OperationOnComponents = MathOperation.Multiply;
            temp_hp_progression.BaseValue = 1;

            var calculateShared = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 0.5,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.CasterCustomProperty,
                        m_CustomProperty = temp_hp_progression.ToRef()
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0
                    }
                }
            };
            var fw_temp_hp = new TemporaryHitPointsUnique
            {
                Value = new ContextValue
                {
                    Value = 1,
                    ValueType = ContextValueType.Shared,
                    ValueShared = AbilitySharedValue.Damage
                },
                RemoveWhenHitPointsEnd = false,
                Descriptor = ModifierDescriptor.UntypedStackable
            };

            var fw_regen = new RegenTempHpPerMinute(kineticist_class, fw_effect_feature);

            fw_effect_feature.SetComponents
                (
                fw_temp_hp,
                calculateShared,
                Helper.CreateRecalculateOnFactsChange(fw_effect_feature.ToRef2()),
                fw_regen
                );
            #endregion
            #region Resource
            var fw_resource = Helper.CreateBlueprintAbilityResource("ForceWardResource", "Force Ward",
                ForceWardDescription, null, false, 20, 0, 3, 0, 0, 0, 0, false, 0, false, 0, StatType.Constitution,
                true, 0, kineticist_class, null);
            #endregion
            #region Buff
            var fw_buff = Helper.CreateBlueprintBuff("ForceWardBuff", "FW Buff",
                null, null, null, null);
            fw_buff.Flags(true, true, null, null)
                .SetComponents
                (
                Helper.CreateAddFacts(fw_effect_feature.ToRef2())
                );
            fw_buff.Stacking = StackingType.Stack;
            var fw_buff_combat_refresh = Helper.CreateBlueprintBuff("ForceWardBuffCombatRefresh", "FW Buff Refresh",
                null, null, icon, null);
            fw_buff.Flags(true, true, null, null)
                .SetComponents
                (
                Helper.CreateAddFacts(fw_effect_feature.ToRef2())
                );
            fw_buff_combat_refresh.Stacking = StackingType.Prolong;

            #endregion
            #region Ability

            var fw_ability = Helper.CreateBlueprintAbility("ForceWardAbility", "Force Ward",
                ForceWardDescription, null, icon, AbilityType.Special, UnitCommand.CommandType.Free,
                AbilityRange.Personal, null, null)
                .SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, fw_buff.CreateContextActionApplyBuff(0, DurationRate.Rounds, false, true, false, true, true)),
                Helper.CreateAbilityResourceLogic(fw_resource.ToRef(), true, false, 1),
                Helper.CreateAbilityAcceptBurnOnCast(1)
                );

            #endregion
            #region Feature
            var fw_feature = Helper.CreateBlueprintFeature("ForceWardFeature", "Force Ward",
                ForceWardDescription, null, null, 0)
                .SetComponents
                (
                Helper.CreateAddFacts(fw_effect_feature.ToRef2(), fw_ability.ToRef2()),
                Helper.CreateAddAbilityResources(false, 0, true, false, fw_resource.ToRef()),
                Helper.CreatePrerequisiteFeaturesFromList(true, tb_feature.ToRef()),
                Helper.CreateCombatStateTrigger(fw_buff_combat_refresh.CreateContextActionApplyBuff(0, DurationRate.Rounds, false, true, false, true, true))
                );

            #endregion

            return fw_feature;
        }

        private static BlueprintFeature CreateForceWard(BlueprintFeature tb_feature)
        {
            var icon = Helper.CreateSprite("forceWard.png");
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391");

            #region Effect Feature

            var effect_feature = Helper.CreateBlueprintFeature("ForceWardEffectFeature", null,
                null, null, icon, FeatureGroup.None);
            effect_feature.Ranks = 20;
            effect_feature.HideInUI = true;
            effect_feature.HideInCharacterSheetAndLevelUp = true;
            effect_feature.IsClassFeature = true;
            effect_feature.SetComponents
                (
                Helper.CreateAddFacts()
                );

            #endregion
            #region Effect Buff

            var effect_buff = Helper.CreateBlueprintBuff("ForceWardEffectBuff", null,
                null, null, icon);
            effect_buff.Flags(hidden: true, stayOnDeath: true, removeOnRest: true);
            effect_buff.Stacking = StackingType.Stack;
            effect_buff.IsClassFeature = true;
            effect_buff.SetComponents
                (
                Helper.CreateAddFacts(effect_feature.ToRef2())
                );

            #endregion
            #region Buff

            var feature_value_getter = new FeatureRankPlusBonusGetter()
            {
                Feature = effect_feature.ToRef(),
                bonus = 4
            };
            var classlvl_value_getter = new ClassLevelGetter()
            {
                ClassRef = kineticist_class
            };
            var temp_hp_progression = Helper.CreateBlueprintUnitProperty("ForceWardHPProperty")
                .SetComponents
                (
                feature_value_getter,
                classlvl_value_getter
                );
            temp_hp_progression.OperationOnComponents = MathOperation.Multiply;
            temp_hp_progression.BaseValue = 1;

            var calculateShared = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 0.5,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.CasterCustomProperty,
                        m_CustomProperty = temp_hp_progression.ToRef()
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0
                    }
                }
            };
            var temp_hp = new TemporaryHitPointsUnique
            {
                Value = new ContextValue
                {
                    Value = 1,
                    ValueType = ContextValueType.Shared,
                    ValueShared = AbilitySharedValue.Damage
                },
                RemoveWhenHitPointsEnd = false,
                Descriptor = ModifierDescriptor.UntypedStackable
            };
            var regen = new RegenTempHpPerMinute(kineticist_class, effect_feature);



            var buff = Helper.CreateBlueprintBuff("ForceWardBuff", null,
                null, null, icon);
            buff.Flags(hidden: true, stayOnDeath: true);
            buff.Stacking = StackingType.Replace;
            buff.IsClassFeature = true;
            buff.SetComponents
                (
                temp_hp,
                regen,
                calculateShared,
                Helper.CreateRecalculateOnFactsChange(effect_feature.ToRef2())
                );

            // TEMP TODO REMOVE
            var fw_buff_combat_refresh = Helper.CreateBlueprintBuff("ForceWardBuffCombatRefresh", "FW Buff Refresh",
            null, null, icon, null);
            fw_buff_combat_refresh.Flags(true, true, null, null)
                .SetComponents
                (
                Helper.CreateAddFacts(effect_feature.ToRef2())
                );
            fw_buff_combat_refresh.Stacking = StackingType.Prolong;
            var fw_resource = Helper.CreateBlueprintAbilityResource("ForceWardResource", "Force Ward",
                ForceWardDescription, null, false, 20, 0, 3, 0, 0, 0, 0, false, 0, false, 0, StatType.Constitution,
                true, 0, kineticist_class, null);
            // TEMP TODO REMOVE

            #endregion
            #region Ability

            var ability = Helper.CreateBlueprintAbility("ForceWardAbility", "Force Ward",
                ForceWardDescription, null, icon, AbilityType.Special, UnitCommand.CommandType.Free,
                AbilityRange.Personal).TargetSelf(CastAnimationStyle.Omni);
            ability.AvailableMetamagic = Metamagic.Heighten;
            ability.SetComponents
                (
                Helper.CreateAbilityEffectRunAction(actions: effect_buff.CreateContextActionApplyBuff(permanent: true)),
                Helper.CreateAbilityAcceptBurnOnCast(1)
                );

            #endregion

            var feature = Helper.CreateBlueprintFeature("ForceWardFeature", "Force Ward",
                ForceWardDescription, null, icon, FeatureGroup.None);
            feature.IsClassFeature = true;
            feature.SetComponents
                (
                Helper.CreateAddFacts(buff.ToRef2(), ability.ToRef2()),
                Helper.CreatePrerequisiteFeaturesFromList(true, tb_feature.ToRef())
                );

            return feature;

        }

        #endregion

        #region Telekinetic Blast
        private static BlueprintFeatureBase CreateFullTelekineticBlast(out BlueprintFeature blast_feature, out BlueprintFeature tb_blade_feature, out BlueprintAbility blast_ability)
        {
            var variant_base = CreateTelekineticBlastVariant_base();
            var variant_extended = CreateTelekineticBlastVariant_extended();
            var variant_spindle = CreateTelekineticBlastVariant_spindle();
            var variant_wall = CreateTelekineticBlastVariant_wall();
            var variant_blade = CreateTelekineticBlastVariant_blade(out tb_blade_feature);
            var variant_throw = CreateTelekineticBlastVariant_throw(out var foeThrowInfusion);
            var variant_many = CreateTelekineticBlastVariant_many(out var manyThrowInfusion);
            blast_ability = CreateTelekineticBlastAbility(variant_base, variant_many, variant_extended, variant_spindle, variant_wall, variant_blade);
            blast_feature = CreateTelekineticBlastFeature(blast_ability, tb_blade_feature);
            var blast_progression = CreateTelekineticBlastProgression(blast_feature, tb_blade_feature);

            AddToKineticBladeInfusion(tb_blade_feature, blast_feature);
            AddToSubstanceInfusions(blast_feature, blast_ability);
            AddBlastsToMetakinesis(blast_ability);
            AddBlastsToBurn(blast_ability);
            AddBlastsToBurn(variant_throw);
            SetInfusionPrereqs(foeThrowInfusion, blast_feature.ToRef());
            SetInfusionPrereqs(manyThrowInfusion, blast_feature.ToRef());
            try
            {
                var extra_wild = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("bd287f6d1c5247da9b81761cab64021c"); // DarkCodex's ExtraWildTalentFeat
                Helper.AppendAndReplace(ref extra_wild.m_AllFeatures, new List<BlueprintFeatureReference> { foeThrowInfusion.ToRef(), manyThrowInfusion.ToRef() });
            }
            catch (Exception ex)
            {
                Helper.Print($"Dark Codex not installed: {ex.Message}");
            }
            return blast_progression;
        }

        private static BlueprintAbility CreateTelekineticBlastAbility(params BlueprintAbility[] variants)
        {
            var icon = Helper.CreateSprite("telekineticBlast.png");

            var ability = Helper.CreateBlueprintAbility("TelekineticBlastBase",
                "Telekinetic Blast", TelekineticBlastDescription,
                null, icon, AbilityType.Special, UnitCommand.CommandType.Standard,
                AbilityRange.Close, duration: null, savingThrow: null);
            ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact("1f3a15a3ae8a5524ab8b97f469bf4e3d".ToRef<BlueprintUnitFactReference>()), // ElementalFocusSelection
                Step5_burn(null, 0, 0, 0),
                Helper.CreateSpellDescriptorComponent(SpellDescriptor.Force)
                );
            ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            foreach (var v in variants)
            {
                Helper.AddToAbilityVariants(ability, v);
            }

            return ability;
        }

        private static BlueprintFeature CreateTelekineticBlastFeature(BlueprintAbility blast_ability, BlueprintFeature blade_feature)
        {
            var blade_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("9ff81732-dadd-b174-aa81-38ad1297c787"); // KineticBladeInfusion

            var feature = Helper.CreateBlueprintFeature("TelekineticBlastFeature",
                "Telekinetic Blast", TelekineticBlastDescription,
                null, null, FeatureGroup.KineticBlast)
                .SetComponents
                (
                Helper.CreateAddFacts(blast_ability.ToRef2()),
                Helper.CreateAddFeatureIfHasFact(blade_infusion.ToRef2(), blade_feature.ToRef2())
                );
            feature.HideInUI = true;
            return feature;
        }

        private static BlueprintFeatureBase CreateTelekineticBlastProgression(BlueprintFeature blast_feature, BlueprintFeature blade_feature)
        {
            var kinetic_blade_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("9ff81732-dadd-b174-aa81-38ad1297c787"); // KineticBladeInfusion
            var composite_blast_buff = ResourcesLibrary.TryGetBlueprint<BlueprintUnitFact>("cb30a291-c75d-ef84-0904-30fbf2b5c05e");

            var progression = Helper.CreateBlueprintProgression("TelekineticBlastProgression", "Telekinetic Blast",
                TelekineticBlastDescription, null, null, 0)
                .SetComponents
                (
                Helper.CreateAddFacts(composite_blast_buff.ToRef()),
                Helper.CreateAddFeatureIfHasFact(kinetic_blade_infusion.ToRef2(), blade_feature.ToRef2()),
                Helper.CreateAddFeatureIfHasFact(blast_feature.ToRef2())
                );

            var entry = Helper.CreateLevelEntry(1, blast_feature);
            Helper.AddEntries(progression, entry);

            return progression;
        }

        #region Blast Variants

        private static BlueprintAbility CreateTelekineticBlastVariant_base()
        {
            var icon = Helper.CreateSprite("telekineticBlast.png");

            var blast = Helper.CreateBlueprintAbility(
                "TelekineticBlastAbility",
                "Telekinetic Blast",
                TelekineticBlastDescription,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                Step1_run_damage(out var actions,
                p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing,
                isAOE: false, half: false),
                Step2_rank_dice(twice: false),
                Helper.CreateContextCalculateSharedValue(Modifier: 1.0, Value: Helper.CreateContextDiceValue(DiceType.One, AbilityRankType.DamageDice, AbilityRankType.DamageBonus)),
                Step3_rank_bonus(half_bonus: false),
                Step4_dc(),
                Step5_burn(actions, infusion: 0, blast: 0),
                Step8_spell_description(SpellDescriptor.Hex),
                Step7_projectile(Resource.Projectile.BatteringBlast00, true, AbilityProjectileType.Simple, 0, 5),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            // Bandaids
            ((ContextActionDealDamage)actions.Actions[0]).UseWeaponDamageModifiers = true;
            ((ContextActionDealDamage)actions.Actions[0]).Value.BonusValue.ValueType = ContextValueType.Shared;

            return blast;
        }

        private static BlueprintAbility CreateTelekineticBlastVariant_extended()
        {
            var requirement = Helper.ToRef<BlueprintUnitFactReference>("cb2d9e63-55dd-3394-0b2b-ef49e544b0bf");
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("cb2d9e63-55dd-3394-0b2b-ef49e544b0bf");
            var weapon = Helper.ToRef<BlueprintItemWeaponReference>("65951e11-9584-8844-b8ab-8f46d942f6e8");
            var icon = Helper.StealIcon("cb2d9e63-55dd-3394-0b2b-ef49e544b0bf");

            var blast = Helper.CreateBlueprintAbility(
                parent.name+"Telekinetic",
                parent.m_DisplayName,
                parent.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Long,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                Step1_run_damage(out var actions,
                p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing,
                isAOE: false, half: false),
                Step2_rank_dice(twice: false),
                Helper.CreateContextCalculateSharedValue(Modifier: 1.0, Value: Helper.CreateContextDiceValue(DiceType.One, AbilityRankType.DamageDice, AbilityRankType.DamageBonus)),
                Step3_rank_bonus(half_bonus: false),
                Step4_dc(),
                Step5_burn(actions, infusion: 1, blast: 0),
                Helper.CreateAbilityShowIfCasterHasFact(requirement),
                Step7_projectile(Resource.Projectile.BatteringBlast00, true, AbilityProjectileType.Simple, 0, 5),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            ((ContextActionDealDamage)actions.Actions[0]).Value.BonusValue.ValueType = ContextValueType.Shared;

            return blast;
        }

        private static BlueprintAbility CreateTelekineticBlastVariant_spindle()
        {
            var requirement = Helper.ToRef<BlueprintUnitFactReference>("c4f4a62a-325f-7c14-dbca-ce3ce34782b5");
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c4f4a62a-325f-7c14-dbca-ce3ce34782b5");
            var weapon = Helper.ToRef<BlueprintItemWeaponReference>("65951e11-9584-8844-b8ab-8f46d942f6e8");
            var icon = Helper.StealIcon("c4f4a62a-325f-7c14-dbca-ce3ce34782b5");

            var blast = Helper.CreateBlueprintAbility(
                parent.name + "Telekinetic",
                parent.m_DisplayName,
                parent.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                Step1_run_damage(out var actions,
                p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing,
                isAOE: false, half: false, save: SavingThrowType.Reflex),
                Step2_rank_dice(twice: false),
                Helper.CreateContextCalculateSharedValue(Modifier: 1.0, Value: Helper.CreateContextDiceValue(DiceType.One, AbilityRankType.DamageDice, AbilityRankType.DamageBonus)),
                Step3_rank_bonus(half_bonus: false),
                Step4_dc(),
                Step5_burn(null, infusion: 2, blast: 0),
                Helper.CreateAbilityShowIfCasterHasFact(requirement),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth),
                new AbilityDeliverChain 
                { 
                    m_ProjectileFirst = Resource.Projectile.BatteringBlast00_Up.ToRef<BlueprintProjectileReference>(), 
                    m_Projectile = Resource.Projectile.BatteringBlast00.ToRef<BlueprintProjectileReference>(),
                    TargetsCount = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 70,
                        ValueRank = AbilityRankType.ProjectilesCount,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    Radius = new Feet { m_Value = 5 },
                    TargetDead = false,
                    m_TargetType = TargetType.Enemy,
                    m_Condition = new ConditionsChecker { Conditions = null, Operation = Operation.And}
                }
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, Helper.CreateContextValue(AbilityRankType.DamageDice), Helper.CreateContextValue(AbilitySharedValue.Damage));
            var action_damage = Helper.CreateContextActionDealDamage(PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing, dice, sharedValue: AbilitySharedValue.DurationSecond);
            var context_conditional_saved = Helper.CreateContextActionConditionalSaved(null, action_damage);
            actions.Actions = new GameAction[] { context_conditional_saved };

            return blast;
        }

        private static BlueprintAbility CreateTelekineticBlastVariant_wall()
        {
            var requirement = Helper.ToRef<BlueprintUnitFactReference>("c6843359-1889-6ce4-ab13-e96cec929796");
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c6843359-1889-6ce4-ab13-e96cec929796");
            var weapon = Helper.ToRef<BlueprintItemWeaponReference>("65951e11-9584-8844-b8ab-8f46d942f6e8");
            var icon = Helper.StealIcon("c6843359-1889-6ce4-ab13-e96cec929796");
            var area_effect = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityAreaEffect>("2a90aa7f-7716-77b4-e962-4fa77697fdc6");

            var action = new ContextActionSpawnAreaEffect
            {
                DurationValue = Helper.CreateContextDurationValue(
                    new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0,
                        ValueRank = AbilityRankType.Default,
                        ValueShared = AbilitySharedValue.Damage
                    }, DiceType.Zero,
                    new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageBonus,
                        ValueShared = AbilitySharedValue.Damage
                    }, DurationRate.Rounds),
                m_AreaEffect = CreateTelekineticWallEffect().ToRef(),
                OnUnit = false
            };

            var blast = Helper.CreateBlueprintAbility(
                parent.name + "Telekinetic",
                parent.m_DisplayName,
                parent.m_Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action),
                Step4_dc(),
                Step5_burn(null, infusion: 3, blast: 0),
                Helper.CreateAbilityShowIfCasterHasFact(requirement),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.CanTargetPoint = true;
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        private static BlueprintAbility CreateTelekineticBlastVariant_many(out BlueprintFeature manyThrowInfusion)
        {
            manyThrowInfusion = CreateManyThrowInfusion();
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391"); // Kineticist Base Class
            var icon = Helper.CreateSprite("manyThrow.png");


            var blast = Helper.CreateBlueprintAbility("ManyThrowTelekineticBlast", manyThrowInfusion.m_DisplayName,
                manyThrowInfusion.m_Description, null, null, AbilityType.SpellLike, UnitCommand.CommandType.Standard,
                AbilityRange.Long);
            blast.SetComponents
                (
                Step1_run_damage(out var actions,
                p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing,
                isAOE: false, half: false),
                Step2_rank_dice(twice: false, half: false),
                Helper.CreateContextCalculateSharedValue(Modifier: 1.0, Value: Helper.CreateContextDiceValue(DiceType.One, AbilityRankType.DamageDice, AbilityRankType.DamageBonus)),
                Step3_rank_bonus(half_bonus: false),
                Step5_burn(actions, infusion: 4, blast: 0, talent: 0),
                Step6_feat(manyThrowInfusion),
                Step8_spell_description(SpellDescriptor.Hex),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth),
                new AbilityDeliverMultiAttack
                {
                    Condition = null,
                    Weapon = "65951e1195848844b8ab8f46d942f6e8".ToRef<BlueprintItemWeaponReference>(),
                    Projectiles = new BlueprintProjectileReference[]
                        { 
                            Resource.Projectile.MagicMissile00.ToRef<BlueprintProjectileReference>(),
                            Resource.Projectile.MagicMissile01.ToRef<BlueprintProjectileReference>(),
                            Resource.Projectile.MagicMissile02.ToRef<BlueprintProjectileReference>(),
                            Resource.Projectile.MagicMissile03.ToRef<BlueprintProjectileReference>(),
                            Resource.Projectile.MagicMissile04.ToRef<BlueprintProjectileReference>(),
                        },
                    TargetType = TargetType.Enemy,
                    DelayBetweenChain = 0f,
                    radius = new Feet { m_Value = 30 },
                    TargetsCount = Helper.CreateContextValue(AbilityRankType.ProjectilesCount)
                },
                Helper.CreateContextRankConfig(ContextRankBaseValueType.ClassLevel, ContextRankProgression.AsIs, type: AbilityRankType.ProjectilesCount,
                    classes: new BlueprintCharacterClassReference[] { kineticist_class })
                ).TargetPoint(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            ((ContextActionDealDamage)actions.Actions[0]).Value.BonusValue.ValueType = ContextValueType.Shared;

            return blast;
        }

        private static BlueprintAbility CreateTelekineticBlastVariant_throw(out BlueprintFeature foeThrowInfusion)
        {
            var icon = Helper.CreateSprite("foeThrow.png");

            foeThrowInfusion = CreateFoeThrowInfusion();
            var foeThrowBuff = CreateFoeThrowTargetBuff();
            var ft_targetAbility = CreateFoeThrowTargetAbility(foeThrowBuff, foeThrowInfusion);
            var ft_throwAbility = CreateFoeThrowThrowAbility(foeThrowBuff, foeThrowInfusion);

            var blast = Helper.CreateBlueprintAbility("FoeThrowTelekineticBlast", foeThrowInfusion.m_DisplayName,
                foeThrowInfusion.m_Description, null, icon, AbilityType.Special, UnitCommand.CommandType.Standard,
                AbilityRange.Close, null, null);
            blast.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact("1f3a15a3ae8a5524ab8b97f469bf4e3d".ToRef<BlueprintUnitFactReference>()), // ElementalFocusSelection
                Step5_burn(null, infusion: 2, blast: 0, 0),
                Helper.CreateSpellDescriptorComponent(SpellDescriptor.Force)
                );
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            Helper.AddToAbilityVariants(blast, ft_targetAbility);
            Helper.AddToAbilityVariants(blast, ft_throwAbility);

            foeThrowInfusion.AddComponents(Helper.CreateAddFacts(blast.ToRef2()));

            return blast;
        }

        #endregion

        #region Telekinetic Blade
        private static BlueprintAbility CreateTelekineticBlastVariant_blade(out BlueprintFeature tb_blade_feat)
        {
            var kinetic_blade_enable_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("426a9c07-9ee7-ac34-aa8e-0054f2218074"); // KineticBladeEnableBuff
            var kinetic_blade_hide_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("4d39ccef-7b5b-2e94-58e8-599eae3c3be0"); // KineticBladeHideFeature
            var icon = Helper.StealIcon("89acea31-3b9a-9cb4-d86b-bbca01b90346"); // KineticBladeAirBlastAbility
            var damage_icon = Helper.StealIcon("89cc522f-2e14-44b4-0ba1-757320c58530"); // AirBlastKineticBladeDamage

            var weapon = CreateTelekineticBlastBlade_weapon();

            #region buffs
            var buff = Helper.CreateBlueprintBuff("KineticBladeTelekineticBlastBuff", null, null, null, null, null);
            buff.Flags(true, true, null, null);
            buff.Stacking = StackingType.Replace;
            buff.SetComponents
                (
                new AddKineticistBlade { m_Blade = weapon.ToRef() }
                );
            #endregion

            #region KineticBladeTelekineticBlastAbility

            var blade_active_ability = Helper.CreateBlueprintActivatableAbility("KineticBladeTelekineticBlastAbility", "Telekinetic Blast — Kinetic Blade",
                KineticBladeDescription, out var unused, null, icon,
                group: Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityGroup.FormInfusion, deactivateWhenDead: true);
            blade_active_ability.m_Buff = buff.ToRef();
            blade_active_ability.m_ActivateOnUnitAction = Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivateOnUnitActionType.Attack;
            blade_active_ability.SetComponents
                (
                new RestrictionCanUseKineticBlade { }
                );

            #endregion

            #region KineticBladeTelekineticBlastBurnAbility

            var blade_burn_ability = Helper.CreateBlueprintAbility("KineticBladeTelekineticBlastBurnAbility", null, null, null, icon,
                AbilityType.Special, UnitCommand.CommandType.Free, AbilityRange.Personal);
            blade_burn_ability.TargetSelf(CastAnimationStyle.Omni);
            blade_burn_ability.Hidden = true;
            blade_burn_ability.DisableLog = true;
            blade_burn_ability.AvailableMetamagic = Metamagic.Extend | Metamagic.Heighten;
            blade_burn_ability.SetComponents
                (
                new AbilityKineticist { Amount = 1, InfusionBurnCost = 1 },
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, kinetic_blade_enable_buff.CreateContextActionApplyBuff(asChild: true)),
                new AbilityKineticBlade { }
                );
            AddBlastsToMetakinesis(blade_burn_ability);

            #endregion

            #region TelekineticBlastKineticBladeDamage

            var blade_damage_ability = Helper.CreateBlueprintAbility("TelekineticBlastKineticBladeDamage", "Telekinetic Blast",
                TelekineticBlastDescription, null, damage_icon, AbilityType.Special, UnitCommand.CommandType.Standard, AbilityRange.Close);
            blade_damage_ability.TargetEnemy(CastAnimationStyle.Omni);
            blade_damage_ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;
            blade_damage_ability.Hidden = true;
            blade_damage_ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact(kinetic_blade_hide_feature.ToRef2()),
                new AbilityDeliveredByWeapon { },
                Step1_run_damage(out var actions, p: PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing),
                Step2_rank_dice(false, false),
                Step3_rank_bonus(false),
                Step4_dc(),
                Step5_burn(actions, infusion: 1),
                Step7_projectile(Resource.Projectile.WindProjectile00, true, AbilityProjectileType.Simple, 0, 5),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                );
            blade_damage_ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            #endregion

            weapon.SetComponents
                (
                new WeaponKineticBlade { m_ActivationAbility = blade_burn_ability.ToRef(), m_Blast = blade_damage_ability.ToRef()}
                );

            // Blast Burn/Blast Ability (active)
            tb_blade_feat = Helper.CreateBlueprintFeature("TelekineticKineticBladeFeature", null, null, null, icon, FeatureGroup.None);
            tb_blade_feat.HideInUI = true;
            tb_blade_feat.HideInCharacterSheetAndLevelUp = true;
            tb_blade_feat.SetComponents
                (
                Helper.CreateAddFeatureIfHasFact(blade_active_ability.ToRef()),
                Helper.CreateAddFeatureIfHasFact(blade_burn_ability.ToRef2())
                );

            TeleBladeBuff = buff;

            return blade_damage_ability;
        }

        private static BlueprintItemWeapon CreateTelekineticBlastBlade_weapon()
        {
            //var icon = Helper.StealIcon("43ff6714-3efb-86d4-f894-b10577329050"); // Air Kinetic Blade Weapon
            var kinetic_blast_physical_blade_type = Helper.ToRef<BlueprintWeaponTypeReference>("b05a206f-6c11-33a4-69b2-f7e30dc970ef"); // Kinetic Blast Physical Blade Type
            
            var weapon = Helper.CreateBlueprintItemWeapon("AetherKineticBladeWeapon", null, null, kinetic_blast_physical_blade_type,
                damageOverride: new DiceFormula { m_Rolls = 0, m_Dice = DiceType.Zero },
                form: null,
                secondWeapon: null, false, null, 10);
            weapon.m_Enchantments = new BlueprintWeaponEnchantmentReference[1] { CreateTelekineticBlastBlade_enchantment().ToRef() };

            weapon.m_VisualParameters.m_WeaponAnimationStyle = Kingmaker.View.Animation.WeaponAnimationStyle.SlashingOneHanded;
            weapon.m_VisualParameters.m_SpecialAnimation = Kingmaker.Visual.Animation.Kingmaker.UnitAnimationSpecialAttackType.None;
            weapon.m_VisualParameters.m_WeaponModel = new PrefabLink { AssetId = "7c05296dbc70bf6479e66df7d9719d1e" };
            weapon.m_VisualParameters.m_WeaponBeltModelOverride = null;
            weapon.m_VisualParameters.m_WeaponSheathModelOverride = new PrefabLink { AssetId = "f777a23c850d099428c33807f83cd3d6" };

            // Components are later
            return weapon;
        }

        private static BlueprintWeaponEnchantment CreateTelekineticBlastBlade_enchantment()
        {
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2-764b-5504-e98e-6824cab3d27c"); // Kinetic Blast Feature
            var kineticist_main_stat_property = Helper.ToRef<BlueprintUnitPropertyReference>("f897845b-bbc0-08d4-f9c1-c4a03e22357a"); // Kineticist Main Stat Property

            var first_context_calc = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = 0,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    }
                }
            };
            var first_rank_conf = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, type: AbilityRankType.DamageDice, feature: kinetic_blast_feature.ToRef(), min: 0, max: 20);
            var second_rank_conf = Helper.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, type: AbilityRankType.DamageBonus, customProperty: kineticist_main_stat_property, min: 0, max: 20);
            var second_context_calc = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.DamageBonus,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Shared,
                        Value = 0,
                        ValueRank = AbilityRankType.Default,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageBonus,
                        ValueShared = AbilitySharedValue.Damage
                    }
                }
            };

            var enchant = Helper.CreateBlueprintWeaponEnchantment("AetherKineticBladeEnchantment", "Telekinetic Blast — Kinetic Blade",
                null, "Telekinetic Blast", null, null, 0);
            enchant.SetComponents
                (
                first_context_calc,
                first_rank_conf,
                second_rank_conf,
                second_context_calc
                );
            enchant.WeaponFxPrefab = new PrefabLink { AssetId = "19d9b36b62efe1448b00630ec53db58c" };

            return enchant;
        }

        #endregion

        #region Blast Helpers

        private static void AddElementalDefenseIsPrereqFor(BlueprintFeature blast_feature, BlueprintFeature tb_blade_feature, BlueprintFeature fw_feature)
        {
            blast_feature.IsPrerequisiteFor = Helper.ToArray(fw_feature).ToRef().ToList();
            tb_blade_feature.IsPrerequisiteFor = Helper.ToArray(fw_feature).ToRef().ToList();
        }

        private static void AddToKineticBladeInfusion(BlueprintFeature blade_feature, BlueprintFeature blast_feature)
        {
            var kinetic_blade_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("9ff81732-dadd-b174-aa81-38ad1297c787"); // KineticBladeInfusion
            kinetic_blade_infusion.AddComponents(Helper.CreateAddFeatureIfHasFact(blast_feature.ToRef2(), blade_feature.ToRef2()));
        }

        private static void AddToSubstanceInfusions(BlueprintFeature blast_feature, BlueprintAbility blast_ability)
        {
            var bowlingInfusion_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("b3bd080e-ed83-a994-0abd-97e4aa2a7341"); // BowlingInfusionFeature
            var bowlingInfusion_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("918b2524-af5c-3f64-7b5d-aa4f4e985411"); // BowlingInfusionBuff
            var pushingInfusion_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("fbb97f35-a41b-71c4-cbc3-6c5f3995b892"); // PushingInfusionFeature
            var pushingInfusion_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f795bede-8bae-faf4-d9d7-f404ede960ba"); // PushingInfusionBuff

            var prereq = bowlingInfusion_feature.GetComponent<PrerequisiteFeaturesFromList>();
            Helper.AppendAndReplace(ref prereq.m_Features, blast_feature.ToRef());
            prereq = pushingInfusion_feature.GetComponent<PrerequisiteFeaturesFromList>();
            Helper.AppendAndReplace(ref prereq.m_Features, blast_feature.ToRef());

            var applicable = bowlingInfusion_buff.GetComponent<AddKineticistBurnModifier>();
            Helper.AppendAndReplace(ref applicable.m_AppliableTo, blast_ability.ToRef());
            applicable = pushingInfusion_buff.GetComponent<AddKineticistBurnModifier>();
            Helper.AppendAndReplace(ref applicable.m_AppliableTo, blast_ability.ToRef());

            var trigger = bowlingInfusion_buff.GetComponent<AddKineticistInfusionDamageTrigger>();
            Helper.AppendAndReplace(ref trigger.m_AbilityList, blast_ability.ToRef());
            trigger = pushingInfusion_buff.GetComponent<AddKineticistInfusionDamageTrigger>();
            Helper.AppendAndReplace(ref trigger.m_AbilityList, blast_ability.ToRef());
        }

        #endregion

        #endregion

        #region Composite Blast

        private static BlueprintFeature CreateAetherCompositeBlasts(out BlueprintFeature force_blade_feature, out BlueprintAbility force_blast_ability, out BlueprintFeature lesserAethericBoost, out BlueprintFeature greaterAethericBoost, out BlueprintBuff lesserAethericBuff, out BlueprintBuff greaterAethericBuff)
        {
            var variant_base = CreateForceBlastVariant_base();
            var variant_extended = CreateForceBlastVariant_extended();
            var variant_spindle = CreateForceBlastVariant_spindle();
            var variant_wall = CreateForceBlastVariant_wall();
            var variant_blade = CreateForceBlastVariant_blade(out force_blade_feature, out var force_blade_burn);
            var variant_hook = CreateForceBlastVariant_hook(out var forceHookInfusion);
            force_blast_ability = CreateForceBlastAbility(variant_base, variant_hook, variant_extended, variant_spindle, variant_wall, variant_blade);
            var force_blast_feature = CreateForceBlastFeature(force_blast_ability, force_blade_feature);

            AddToKineticBladeInfusion(force_blade_feature, force_blast_feature);
            AddToSubstanceInfusions(force_blast_feature, force_blast_ability);
            AddInfusions(force_blast_feature, force_blast_ability, force_blade_burn);
            AddBlastsToMetakinesis(force_blast_ability);
            AddBlastsToBurn(force_blast_ability);
            SetInfusionPrereqs(forceHookInfusion, force_blast_feature.ToRef());
            try
            {
                var extra_wild = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("bd287f6d1c5247da9b81761cab64021c"); // DarkCodex's ExtraWildTalentFeat
                Helper.AppendAndReplace(ref extra_wild.m_AllFeatures, new List<BlueprintFeatureReference> { forceHookInfusion.ToRef() });
            }
            catch (Exception ex)
            {
                Helper.Print($"Dark Codex not installed: {ex.Message}");
            }

            CreateAethericBoost(out lesserAethericBoost, out greaterAethericBoost, out lesserAethericBuff, out greaterAethericBuff);

            return force_blast_feature;
        }      

        private static BlueprintAbility CreateForceBlastAbility(params BlueprintAbility[] variants)
        {
            var icon = Helper.CreateSprite("forceBlast.png");

            var ability = Helper.CreateBlueprintAbility("ForceBlastBase",
                "Force Blast", ForceBlastDescription,
                null, icon, AbilityType.Special, UnitCommand.CommandType.Standard,
                AbilityRange.Close, duration: null, savingThrow: null);
            ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact("1f3a15a3ae8a5524ab8b97f469bf4e3d".ToRef<BlueprintUnitFactReference>()), // ElementalFocusSelection
                Step5_burn(null, 0, 2, 0),
                Helper.CreateSpellDescriptorComponent(SpellDescriptor.Force)
                );
            ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            foreach (var v in variants)
            {
                Helper.AddToAbilityVariants(ability, v);
            }

            return ability;
        }

        private static BlueprintFeature CreateForceBlastFeature(BlueprintAbility blast_ability, BlueprintFeature blade_feature)
        {
            var blade_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("9ff81732-dadd-b174-aa81-38ad1297c787"); // KineticBladeInfusion

            var feature = Helper.CreateBlueprintFeature("ForceBlastFeature", "Force Blast",
                ForceBlastDescription, null, null, FeatureGroup.None);
            feature.HideInCharacterSheetAndLevelUp = true;
            feature.HideInUI = true;
            feature.SetComponents
                (
                Helper.CreateAddFacts(blast_ability.ToRef2()),
                Helper.CreateAddFeatureIfHasFact(blade_infusion.ToRef2(), blade_feature.ToRef2())
                );

            return feature;
        }

        #region composite variants

        public static AbilityEffectRunAction CreateForceBlastRunAction()
        {
            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilityRankType.DamageBonus);
            var action_damage = Helper.CreateContextActionDealDamageForce(DamageEnergyType.Fire, dice, sharedValue: AbilitySharedValue.DurationSecond);
            var runaction = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action_damage);

            return runaction;
        }

        public static BlueprintAbility CreateForceBlastVariant_base()
        {
            var icon = Helper.StealIcon("3baf0164-9a92-ae64-0927-b0f633db7c11"); // SteamBlastBase

            var blast = Helper.CreateBlueprintAbility(
                "ForceBlastAbility",
                "Force Blast",
                ForceBlastDescription,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                CreateForceBlastRunAction(), // Force Damage (Force with fire, same as battering blast)
                Step2_rank_dice(twice: true),
                Step3_rank_bonus(half_bonus: true),
                Step4_dc(),
                Step5_burn(null, infusion: 0, blast: 2),
                Step7_projectile(Resource.Projectile.Disintegrate00, false, AbilityProjectileType.Simple, 0, 5),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        public static BlueprintAbility CreateForceBlastVariant_extended()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("cb2d9e63-55dd-3394-0b2b-ef49e544b0bf"); // ExtendedRangeInfusion
            var icon = Helper.StealIcon("cb2d9e63-55dd-3394-0b2b-ef49e544b0bf"); // ExtendedRangeSteamBlastAbility

            var blast = Helper.CreateBlueprintAbility(
                "ExtendedRangeForceBlastAbility",
                parent.m_DisplayName,
                parent.Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Long,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                CreateForceBlastRunAction(), // Force Damage (Force with fire, same as battering blast)
                Step2_rank_dice(twice: true),
                Step3_rank_bonus(half_bonus: true),
                Step4_dc(),
                Step5_burn(null, infusion: 1, blast: 2),
                Step6_feat(parent),
                Step7_projectile(Resource.Projectile.Disintegrate00, false, AbilityProjectileType.Simple, 0, 5),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        public static BlueprintAbility CreateForceBlastVariant_spindle()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c4f4a62a-325f-7c14-dbca-ce3ce34782b5"); // SpindleInfusion
            var icon = Helper.StealIcon("c4f4a62a-325f-7c14-dbca-ce3ce34782b5"); // SpindleInfusion

            var force_runAction = CreateForceBlastRunAction();
            var context_dealDamage = force_runAction.Actions.Actions[0];
            var context_conditional_saved = Helper.CreateContextActionConditionalSaved(null, context_dealDamage);
            force_runAction.Actions.Actions = new GameAction[] { context_conditional_saved };

            var blast = Helper.CreateBlueprintAbility(
                "SplindleForceBlastAbility",
                parent.m_DisplayName,
                parent.Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Close,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                force_runAction,
                Step2_rank_dice(twice: true),
                Step3_rank_bonus(half_bonus: true),
                Step4_dc(),
                Step5_burn(null, infusion: 2, blast: 2),
                Step6_feat(parent),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth),
                new AbilityDeliverChain
                {
                    m_ProjectileFirst = Resource.Projectile.Disintegrate00.ToRef<BlueprintProjectileReference>(),
                    m_Projectile = Resource.Projectile.Disintegrate00.ToRef<BlueprintProjectileReference>(),
                    TargetsCount = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 70,
                        ValueRank = AbilityRankType.ProjectilesCount,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    Radius = new Feet { m_Value = 5 },
                    TargetDead = false,
                    m_TargetType = TargetType.Enemy,
                    m_Condition = new ConditionsChecker { Conditions = null, Operation = Operation.And }
                }
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        public static BlueprintAbility CreateForceBlastVariant_wall()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c6843359-1889-6ce4-ab13-e96cec929796"); // WallInfusion
            var icon = Helper.StealIcon("c6843359-1889-6ce4-ab13-e96cec929796"); // WallInfusion
            var area_effect = ResourcesLibrary.TryGetBlueprint<BlueprintAbilityAreaEffect>("6a64cc20-d582-0dc4-cb39-07b36ce6ac13"); // WallSteamBlastArea

            var action = new ContextActionSpawnAreaEffect
            {
                DurationValue = Helper.CreateContextDurationValue(
                    new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0,
                        ValueRank = AbilityRankType.Default,
                        ValueShared = AbilitySharedValue.Damage
                    }, DiceType.Zero,
                    new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageBonus,
                        ValueShared = AbilitySharedValue.Damage
                    }, DurationRate.Rounds),
                m_AreaEffect = CreateForceWallEffect().ToRef(),
                OnUnit = false
            };

            var blast = Helper.CreateBlueprintAbility(
                "WallForceBlastAbility",
                parent.m_DisplayName,
                parent.Description,
                null,
                icon,
                AbilityType.SpellLike,
                UnitCommand.CommandType.Standard,
                AbilityRange.Long,
                duration: null,
                savingThrow: null)
                .SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action),
                //runaction, // Force Damage (Force with fire, same as battering blast)
                //Step2_rank_dice(twice: false),
                //Step3_rank_bonus(half_bonus: false),
                Step4_dc(),
                Step5_burn(null, infusion: 1, blast: 2),
                Step6_feat(parent),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.CanTargetPoint = true;
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        public static BlueprintAbility CreateForceBlastVariant_hook(out BlueprintFeature forceHookInfusion)
        {
            forceHookInfusion = CreateForceHookInfusion();

            var blast = Helper.CreateBlueprintAbility("ForceHookForceBlastAbility",forceHookInfusion.m_DisplayName,
                forceHookInfusion.Description,null,null,AbilityType.SpellLike,UnitCommand.CommandType.Standard,
                AbilityRange.Close,duration: null,savingThrow: null)
                .SetComponents
                (
                CreateForceBlastRunAction(), // Force Damage (Force with fire, same as battering blast)
                Step2_rank_dice(twice: true),
                Step3_rank_bonus(half_bonus: true),
                Step4_dc(),
                Step5_burn(null, infusion: 2, blast: 2),
                Step6_feat(forceHookInfusion),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth),
                new AbilityCustomMoveToTarget
                {
                    m_Projectile = Resource.Projectile.Disintegrate00.ToRef<BlueprintProjectileReference>(),
                    DisappearFx = new PrefabLink { AssetId = "5caa897344a18ea4e9f7e3368eb2f19b" },
                    DisappearDuration = 0.1f,
                    AppearFx = new PrefabLink { AssetId = "4fa8c88064e270a4594f534c2a65198d" },
                    AppearDuration = 0.1f
                    }
                ).TargetEnemy(CastAnimationStyle.Kineticist);
            blast.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            return blast;
        }

        #region Force Blade

        private static BlueprintAbility CreateForceBlastVariant_blade(out BlueprintFeature force_blade_feat, out BlueprintAbility blade_burn_ability)
        {
            var kinetic_blade_enable_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("426a9c07-9ee7-ac34-aa8e-0054f2218074"); // KineticBladeEnableBuff
            var kinetic_blade_hide_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("4d39ccef-7b5b-2e94-58e8-599eae3c3be0"); // KineticBladeHideFeature
            var icon = Helper.StealIcon("66028030-b968-75b4-c970-66525ff75a27"); // KineticBladeSteamBlastAbility
            var damage_icon = Helper.StealIcon("77dc27ae-2f48-ffe4-a8ab-17154145f1d8"); // SteamBlastBladeDamage

            var weapon = CreateForceBlastBlade_weapon();

            #region buffs
            var buff = Helper.CreateBlueprintBuff("KineticBladeForceBlastBuff", null, null, null, null, null);
            buff.Flags(true, true, null, null);
            buff.Stacking = StackingType.Replace;
            buff.SetComponents
                (
                new AddKineticistBlade { m_Blade = weapon.ToRef() }
                );
            #endregion

            #region KineticBladeForceBlastAbility

            var blade_active_ability = Helper.CreateBlueprintActivatableAbility("KineticBladeForceBlastAbility", "Force Blast — Kinetic Blade",
                KineticBladeDescription, out var unused, null, icon,
                group: Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityGroup.FormInfusion, deactivateWhenDead: true);
            blade_active_ability.m_Buff = buff.ToRef();
            blade_active_ability.m_ActivateOnUnitAction = Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivateOnUnitActionType.Attack;
            blade_active_ability.SetComponents
                (
                new RestrictionCanUseKineticBlade { }
                );

            #endregion

            #region KineticBladeForceBlastBurnAbility

            blade_burn_ability = Helper.CreateBlueprintAbility("KineticBladeForceBlastBurnAbility", null, null, null, icon,
                AbilityType.Special, UnitCommand.CommandType.Free, AbilityRange.Personal);
            blade_burn_ability.TargetSelf(CastAnimationStyle.Omni);
            blade_burn_ability.Hidden = true;
            blade_burn_ability.AvailableMetamagic = Metamagic.Extend | Metamagic.Heighten;
            blade_burn_ability.SetComponents
                (
                new AbilityKineticist { Amount = 1, InfusionBurnCost = 1, BlastBurnCost = 2 },
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, kinetic_blade_enable_buff.CreateContextActionApplyBuff(asChild: true)),
                new AbilityKineticBlade { }
                );
            AddBlastsToMetakinesis(blade_burn_ability);

            #endregion

            #region ForceBlastKineticBladeDamage

            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilityRankType.DamageBonus);
            var action_damage = Helper.CreateContextActionDealDamage(DamageEnergyType.Fire, dice, sharedValue: AbilitySharedValue.DurationSecond);
            action_damage.DamageType.Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Force;
            var runaction = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action_damage);

            var blade_damage_ability = Helper.CreateBlueprintAbility("ForcecBlastKineticBladeDamage", "Force Blast",
                ForceBlastDescription, null, damage_icon, AbilityType.Special, UnitCommand.CommandType.Standard, AbilityRange.Close);
            blade_damage_ability.TargetEnemy(CastAnimationStyle.Omni);
            blade_damage_ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;
            blade_damage_ability.Hidden = true;
            blade_damage_ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact(kinetic_blade_hide_feature.ToRef2()),
                new AbilityDeliveredByWeapon { },
                runaction,
                Step2_rank_dice(false, false),
                Step3_rank_bonus(false),
                Step4_dc(),
                Step5_burn(null, infusion: 0, blast: 0),
                Step7_projectile(Resource.Projectile.Kinetic_SteamLine00, true, AbilityProjectileType.Simple, 0, 5),
                Step8_spell_description(SpellDescriptor.Force),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth)
                );
            blade_damage_ability.AvailableMetamagic = Metamagic.Empower | Metamagic.Maximize | Metamagic.Quicken | Metamagic.Heighten;

            #endregion

            weapon.SetComponents
                (
                new WeaponKineticBlade { m_ActivationAbility = blade_burn_ability.ToRef(), m_Blast = blade_damage_ability.ToRef() }
                );

            force_blade_feat = Helper.CreateBlueprintFeature("ForceKineticBladeFeature", null, null, null, icon, FeatureGroup.None);
            force_blade_feat.HideInUI = true;
            force_blade_feat.HideInCharacterSheetAndLevelUp = true;
            force_blade_feat.SetComponents
                (
                Helper.CreateAddFeatureIfHasFact(blade_active_ability.ToRef()),
                Helper.CreateAddFeatureIfHasFact(blade_burn_ability.ToRef2())
                );

            ForceBladeBuff = buff;

            return blade_damage_ability;
        }

        private static BlueprintItemWeapon CreateForceBlastBlade_weapon()
        {
            //var icon = Helper.StealIcon("43ff6714-3efb-86d4-f894-b10577329050"); // Air Kinetic Blade Weapon
            var kinetic_blast_energy_blade_type = Helper.ToRef<BlueprintWeaponTypeReference>("a15b2fb1-d5dc-4f24-7882-a7148d50afb0"); // Kinetic Blast Energy Blade Type

            var weapon = Helper.CreateBlueprintItemWeapon("ForceKineticBladeWeapon", null, null, kinetic_blast_energy_blade_type,
                damageOverride: new DiceFormula { m_Rolls = 0, m_Dice = DiceType.Zero },
                form: null,
                secondWeapon: null, false, null, 10);
            weapon.m_Enchantments = new BlueprintWeaponEnchantmentReference[1] { CreateForceBlastBlade_enchantment().ToRef() };

            weapon.m_VisualParameters.m_WeaponAnimationStyle = Kingmaker.View.Animation.WeaponAnimationStyle.SlashingOneHanded;
            weapon.m_VisualParameters.m_SpecialAnimation = Kingmaker.Visual.Animation.Kingmaker.UnitAnimationSpecialAttackType.None;
            weapon.m_VisualParameters.m_WeaponModel = new PrefabLink { AssetId = "7c05296dbc70bf6479e66df7d9719d1e" };
            weapon.m_VisualParameters.m_WeaponBeltModelOverride = null;
            weapon.m_VisualParameters.m_WeaponSheathModelOverride = new PrefabLink { AssetId = "f777a23c850d099428c33807f83cd3d6" };

            // Components are later
            return weapon;
        }

        private static BlueprintWeaponEnchantment CreateForceBlastBlade_enchantment()
        {
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2-764b-5504-e98e-6824cab3d27c"); // Kinetic Blast Feature
            var kineticist_main_stat_property = Helper.ToRef<BlueprintUnitPropertyReference>("f897845b-bbc0-08d4-f9c1-c4a03e22357a"); // Kineticist Main Stat Property

            var first_context_calc = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    }
                }
            };
            var first_rank_conf = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, type: AbilityRankType.DamageDice, feature: kinetic_blast_feature.ToRef(), min: 0, max: 20, progression: ContextRankProgression.MultiplyByModifier, stepLevel: 2);
            var second_rank_conf = Helper.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, type: AbilityRankType.DamageBonus, customProperty: kineticist_main_stat_property, min: 0, max: 20, progression: ContextRankProgression.Div2);

            var enchant = Helper.CreateBlueprintWeaponEnchantment("ForceKineticBladeEnchantment", "Force Blast — Kinetic Blade",
                null, "Force Blast", null, null, 0);
            enchant.SetComponents
                (
                first_context_calc,
                first_rank_conf,
                second_rank_conf
                );
            enchant.WeaponFxPrefab = new PrefabLink { AssetId = "fafefd27475150f499b5c7275a851f2f" };

            return enchant;
        }

        #endregion

        #endregion

        // Aetheric Boost (Buff, maybe?)
        //  Provide a buff/toggle with the same scaling as blast dice as bonus damage
        #region Aetheric Boost

        private static void CreateAethericBoost(out BlueprintFeature lesserAethericBoost, out BlueprintFeature greaterAethericBoost, out BlueprintBuff lesserAethericBuff, out BlueprintBuff greaterAethericBuff)
        {
            CreateLesserAethericBoost(out lesserAethericBoost, out lesserAethericBuff);
            CreateGreaterAethericBoost(out greaterAethericBoost, out greaterAethericBuff);
        }

        private static void CreateLesserAethericBoost(out BlueprintFeature lesserAethericBoost, out BlueprintBuff lesserAethericBuff)
        {
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c"); // KineticBlastFeature

            var dice = Helper.CreateContextDiceValue(DiceType.Zero, null, Helper.CreateContextValue(AbilityRankType.DamageBonus));
            var dealDamage = Helper.CreateContextActionDealDamageForce(DamageEnergyType.Fire, dice);

            var trigger = new AddKineticistInfusionDamageTrigger
            {
                Actions = new ActionList { Actions = new GameAction[] { dealDamage } },
                m_WeaponType = null,
                CheckSpellParent = true,
                TriggerOnDirectDamage = true
            };

            var contextRank = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, type: AbilityRankType.DamageBonus, max: 20, feature: kinetic_blast_feature.ToRef());

            var recalc = new RecalculateOnStatChange
            {
                UseKineticistMainStat = true,
                Stat = StatType.Unknown
            };

            lesserAethericBuff = Helper.CreateBlueprintBuff("AethericBoostLesserBuff", "Aetheric Boost",
                AethericBoostLesserDescription, null, null, null);
            lesserAethericBuff.Stacking = StackingType.Replace;
            lesserAethericBuff.Flags(false, true);
            lesserAethericBuff.SetComponents
                (
                trigger, 
                contextRank,
                recalc
                );

            lesserAethericBoost = Helper.CreateBlueprintFeature("AethericBoostLesser", "Aetheric Boost",
                AethericBoostLesserDescription, null, null, FeatureGroup.None);
            lesserAethericBoost.SetComponents
                (
                Helper.CreateAddFactContextActions
                    (
                        new GameAction[] { lesserAethericBuff.CreateContextActionApplyBuff(asChild: true, permanent: true) }
                    )
                );
        }

        private static void CreateGreaterAethericBoost(out BlueprintFeature greaterAethericBoost, out BlueprintBuff GreaterAethericBuff)
        {
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c"); // KineticBlastFeature

            var dice = Helper.CreateContextDiceValue(DiceType.Zero, null, Helper.CreateContextValue(AbilityRankType.DamageBonus));
            var dealDamage = Helper.CreateContextActionDealDamageForce(DamageEnergyType.Fire, dice);

            var trigger = new AddKineticistInfusionDamageTrigger
            {
                Actions = new ActionList { Actions = new GameAction[] { dealDamage } },
                m_WeaponType = null,
                CheckSpellParent = true,
                TriggerOnDirectDamage = true,
            };

            var contextRank = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, progression: ContextRankProgression.MultiplyByModifier, type: AbilityRankType.DamageBonus, stepLevel: 2, feature: kinetic_blast_feature.ToRef());

            var recalc = new RecalculateOnStatChange
            {
                UseKineticistMainStat = true,
                Stat = StatType.Unknown
            };

            GreaterAethericBuff = Helper.CreateBlueprintBuff("AethericBoostGreaterBuff", "Aetheric Boost",
                AethericBoostGreaterDescription, null, null, null);
            GreaterAethericBuff.Stacking = StackingType.Replace;
            GreaterAethericBuff.Flags(false, true);
            GreaterAethericBuff.SetComponents
                (
                trigger,
                contextRank,
                recalc
                );

            greaterAethericBoost = Helper.CreateBlueprintFeature("AethericBoostGreater", "Aetheric Boost",
                AethericBoostGreaterDescription, null, null, FeatureGroup.None);
            greaterAethericBoost.SetComponents
                (
                Helper.CreateAddFactContextActions
                    (
                        new GameAction[] { GreaterAethericBuff.CreateContextActionApplyBuff(asChild: true, permanent: true) }
                    )
                );
        }


        private static void limitAethericBoosts(BlueprintBuff lab, BlueprintBuff gab, BlueprintAbilityReference[] custom_simple, BlueprintAbilityReference[] custom_composite)
        {
            try
            {
                var simple_fire = Helper.ToRef<BlueprintAbilityReference>("83d5873f306ac954cad95b6aeeeb2d8c"); // FireBlastBase
                var simple_earth = Helper.ToRef<BlueprintAbilityReference>("e53f34fb268a7964caf1566afb82dadd"); // EarthBlastBase
                var simple_air = Helper.ToRef<BlueprintAbilityReference>("0ab1552e2ebdacf44bb7b20f5393366d"); // AirBlastBase
                var simple_elec = Helper.ToRef<BlueprintAbilityReference>("45eb571be891c4c4581b6fcddda72bcd"); // ElectricBlastBase
                var simple_water = Helper.ToRef<BlueprintAbilityReference>("d663a8d40be1e57478f34d6477a67270"); // WaterBlastBase
                var simple_cold = Helper.ToRef<BlueprintAbilityReference>("7980e876b0749fc47ac49b9552e259c1"); // ColdBlastBase

                var composite_sand = Helper.ToRef<BlueprintAbilityReference>("b93e1f0540a4fa3478a6b47ae3816f32"); // SandstormBlastBase
                var composite_plasma = Helper.ToRef<BlueprintAbilityReference>("9afdc3eeca49c594aa7bf00e8e9803ac"); // PlasmaBlastBase
                var composite_blizzard = Helper.ToRef<BlueprintAbilityReference>("16617b8c20688e4438a803effeeee8a6"); // BlizzardBlastBase
                var composite_chargeWater = Helper.ToRef<BlueprintAbilityReference>("4e2e066dd4dc8de4d8281ed5b3f4acb6"); // ChargedWaterBlastBase
                var composite_magma = Helper.ToRef<BlueprintAbilityReference>("8c25f52fce5113a4491229fd1265fc3c"); // MagmaBlastBase
                var composite_mud = Helper.ToRef<BlueprintAbilityReference>("e2610c88664e07343b4f3fb6336f210c"); // MudBlastBase
                var composite_steam = Helper.ToRef<BlueprintAbilityReference>("3baf01649a92ae640927b0f633db7c11"); // SteamBlastBase

                custom_simple = custom_simple.Append(simple_air, simple_cold, simple_earth, simple_elec, simple_fire, simple_water);
                custom_composite = custom_composite.Append(composite_blizzard, composite_chargeWater, composite_magma, composite_mud, composite_plasma, composite_sand, composite_steam);

                var lab_trigger = lab.GetComponent<AddKineticistInfusionDamageTrigger>();
                Helper.AppendAndReplace(ref lab_trigger.m_AbilityList, custom_simple);
                var gab_trigger = gab.GetComponent<AddKineticistInfusionDamageTrigger>();
                Helper.AppendAndReplace(ref gab_trigger.m_AbilityList, custom_composite);
            } catch (Exception ex)
            {
                Helper.Print($"Exception: {ex.Message}");
            }
        }

        #endregion

        #endregion

        #region Infusions

        public static void SetInfusionPrereqs(BlueprintFeature infusion, params BlueprintFeatureReference[] blasts)
        {
            infusion.AddComponents(Helper.CreatePrerequisiteFeaturesFromList(true, blasts));
        }

        public static void AddInfusions(BlueprintFeature blast_feature, BlueprintAbility blast_base, BlueprintAbility blade_ability)
        {
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea"); // InfusionSelection

            var disintegrating_infusion = CreateDisintegratingInfusion(blast_feature, blast_base, blade_ability);

            Helper.AppendAndReplace(ref infusion_selection.m_AllFeatures, disintegrating_infusion.ToRef());
            try
            {
                var extra_wild = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("bd287f6d1c5247da9b81761cab64021c"); // DarkCodex's ExtraWildTalentFeat
                Helper.AppendAndReplace(ref extra_wild.m_AllFeatures, new List<BlueprintFeatureReference> { disintegrating_infusion.ToRef() });
            }
            catch (Exception ex)
            {
                Helper.Print($"Dark Codex not installed: {ex.Message}");
            }
        }

        public static BlueprintFeature CreateDisintegratingInfusion(BlueprintFeature blast_feature, BlueprintAbility blast_base, BlueprintAbility blade_burn)
        {
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c"); // KineticBlastFeature
            var kineticist_main_stat_property = "f897845bbbc008d4f9c1c4a03e22357a".ToRef<BlueprintUnitPropertyReference>(); // KineticistMainStatProperty
            var disintegrate_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("f7a6a7d2cfeb36643915aece45349827"); // DisintegrateBuff
            var icon = Helper.StealIcon("4aa7942c3e62a164387a73184bca3fc1"); // Disintegrate Icon

            #region ability

            var ability = Helper.CreateBlueprintActivatableAbility("DisintegratingInfusionAbility", "Disintegrating Infusion",
                DisintegratingInfusionDescription, out var buff, null, icon, activationType: Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivationType.WithUnitCommand,
                deactivateImmediately: true, group: Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityGroup.SubstanceInfusion, onByDefault: true);
            ability.m_ActivateOnUnitAction = Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivateOnUnitActionType.Attack;

            #endregion

            #region Custom Damage

            ContextRankConfig config_dice = Helper.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.FeatureRank, type: AbilityRankType.DamageDice, progression: ContextRankProgression.MultiplyByModifier, stepLevel: 4, feature: kinetic_blast_feature.ToRef());
            ContextRankConfig config_bonus = Helper.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.CustomProperty, type: AbilityRankType.DamageBonus, progression: ContextRankProgression.Div2, stat: StatType.Constitution, customProperty: kineticist_main_stat_property);


            ContextDiceValue value = Helper.CreateContextDiceValue(DiceType.D6, diceCount: Helper.CreateContextValue(AbilityRankType.DamageDice), bonus: Helper.CreateContextValue(AbilityRankType.DamageBonus));

            #endregion

            #region Disintegration

            var apply_buff = disintegrate_buff.CreateContextActionApplyBuff(permanent: true);

            var check_health_less_zero = new ContextConditionCompareTargetHP
            {
                Not = false,
                m_CompareType = ContextConditionCompareTargetHP.CompareType.Less,
                Value = new ContextValue
                {
                    ValueType = ContextValueType.Simple,
                    Value = 0
                }
            };

            var disintegrate_conditional = Helper.CreateConditional(check_health_less_zero, apply_buff);


            #endregion

            #region Buff Components

            var disintegrateNullifyDamage = new AbilityUniqueDisintegrateInfusion(blast_base.ToRef())
            {
                Actions = new ActionList {  Actions = new GameAction[] { disintegrate_conditional } },
                Value = value
            };

            var burn_modifier = new AddKineticistBurnModifier
            {
                BurnType = KineticistBurnType.Infusion,
                Value = 4,
                RemoveBuffOnAcceptBurn = false,
                UseContextValue = false,
                BurnValue = new ContextValue
                {
                    ValueType = ContextValueType.Simple,
                    Value = 0,
                    ValueRank = AbilityRankType.Default,
                    ValueShared = AbilitySharedValue.Damage
                },
                m_AppliableTo = new BlueprintAbilityReference[2] {blast_base.ToRef(), blade_burn.ToRef()}
            };
            var calc_abilityParams = new ContextCalculateAbilityParamsBasedOnClass
            {
                UseKineticistMainStat = true,
                StatType = StatType.Charisma,
                m_CharacterClass = kineticist_class
            };
            var recalc_stat_change = new RecalculateOnStatChange
            {
                UseKineticistMainStat = true,
                Stat = StatType.Unknown
            };

            #endregion

            #region Buff and Feature

            buff.Flags(stayOnDeath: true);
            buff.SetComponents
                (
                disintegrateNullifyDamage,
                config_dice,
                config_bonus,
                calc_abilityParams,
                burn_modifier,
                recalc_stat_change
                );

            var feature = Helper.CreateBlueprintFeature("DisintegratingInfusionFeature", "Disintegrating Infusion",
                DisintegratingInfusionDescription, null, icon, FeatureGroup.KineticBlastInfusion);
            feature.SetComponents
                (
                Helper.CreateAddFacts(ability.ToRef2()),
                Helper.CreatePrerequisiteFeaturesFromList(true, blast_feature.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 12, false)
                );

            #endregion

            return feature;
        }

        public static BlueprintFeature CreateManyThrowInfusion()
        {
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea"); // InfusionSelection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391"); // Kineticist Base Class
            var elemental_focus_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d"); // ElementalFocusSelection
            var extended_range = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("cb2d9e6355dd33940b2bef49e544b0bf"); // ExtendedRangeInfusion
            var icon = Helper.CreateSprite("manyThrow.png");

            var feature = Helper.CreateBlueprintFeature("ManyThrowInfusion", "Many Throw", 
                ManyThrowInfusionDescription, null, icon, FeatureGroup.KineticBlastInfusion);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 16),
                Helper.CreatePrerequisiteFeature(elemental_focus_selection.ToRef()),
                Helper.CreatePrerequisiteFeaturesFromList(false, extended_range.ToRef())
                );

            Helper.AppendAndReplace(ref infusion_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }

        public static BlueprintFeature CreateForceHookInfusion()
        {
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea"); // InfusionSelection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391"); // Kineticist Base Class
            var elemental_focus_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d"); // ElementalFocusSelection

            var feature = Helper.CreateBlueprintFeature("ForceHookInfusion", "Force Hook",
                ForceHookInfusionDescription, null, null, FeatureGroup.KineticBlastInfusion);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 6),
                Helper.CreatePrerequisiteFeature(elemental_focus_selection.ToRef())
                );

            Helper.AppendAndReplace(ref infusion_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }

        #region Foe Throw

        public static BlueprintFeature CreateFoeThrowInfusion()
        {
            var infusion_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("58d6f8e9eea63f6418b107ce64f315ea"); // InfusionSelection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391"); // Kineticist Base Class
            var elemental_focus_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("1f3a15a3ae8a5524ab8b97f469bf4e3d"); // ElementalFocusSelection
            var icon = Helper.CreateSprite("foeThrow.png");

            var feature = Helper.CreateBlueprintFeature("FoeThrowInfusion", "Foe Throw",
                FoeThrowInfusionDescription, null, icon, FeatureGroup.KineticBlastInfusion);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 6),
                Helper.CreatePrerequisiteFeature(elemental_focus_selection.ToRef())
                );

            Helper.AppendAndReplace(ref infusion_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }

        public static BlueprintBuff CreateFoeThrowTargetBuff()
        {
            var icon = Helper.CreateSprite("foeThrow.png");

            var buff = Helper.CreateBlueprintBuff("FoeThrowInfusionTargetBuff", "Lifted",
                FoeThrowTargetBuffDescription, null, icon, null);
            buff.Flags();
            buff.Stacking = StackingType.Replace;
            return buff;
        }

        public static BlueprintAbility CreateFoeThrowTargetAbility(BlueprintBuff foeThrowBuff, BlueprintFeature requirement)
        {
            var icon = Helper.CreateSprite("foeThrow.png");

            var ability = Helper.CreateBlueprintAbility("FoeThrowInfusionTargetAbility", "Lift Target",
                FoeThrowInfusionTargetAbilityDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Free,
                AbilityRange.Close, null, null);
            ability.SetComponents
                (
                Step6_feat(requirement),
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, new ContextActionRemoveBuffAll() { m_Buff = foeThrowBuff }, foeThrowBuff.CreateContextActionApplyBuff(1, DurationRate.Rounds))
                ).TargetEnemy(CastAnimationStyle.Kineticist);
                
            return ability;
        }

        public static BlueprintAbility CreateFoeThrowThrowAbility(BlueprintBuff foeThrowBuff, BlueprintFeature requirement)
        {
            var icon = Helper.CreateSprite("foeThrow.png");

            var ability = Helper.CreateBlueprintAbility("FoeThrowInfusionThrowAbility", "Throw Target",
                FoeThrowInfusionThrowAbilityDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard,
                AbilityRange.Close, null, null);
            ability.SetComponents
                (
                new AbilityCustomFoeThrowUnique
                {
                    m_Projectile = Resource.Projectile.BatteringBlast00.ToRef<BlueprintProjectileReference>(),
                    DisappearFx = new PrefabLink { AssetId = "5caa897344a18ea4e9f7e3368eb2f19b" },
                    DisappearDuration = 0f,
                    AppearFx = new PrefabLink { AssetId = "4fa8c88064e270a4594f534c2a65198d" },
                    AppearDuration = 0f,
                    m_Buff = foeThrowBuff,
                    Value = Helper.CreateContextDiceValue(DiceType.D6, Helper.CreateContextValue(AbilityRankType.DamageDice), Helper.CreateContextValue(AbilitySharedValue.Damage))
                },
                Step2_rank_dice(twice: false, half: false),
                Helper.CreateContextCalculateSharedValue(Modifier: 1.0, Value: Helper.CreateContextDiceValue(DiceType.One, AbilityRankType.DamageDice, AbilityRankType.DamageBonus)),
                Step3_rank_bonus(half_bonus: false),
                Step4_dc(),
                Step5_burn(null, infusion: 2, blast: 0),
                Step6_feat(requirement),
                Step8_spell_description(SpellDescriptor.Hex),
                Step_sfx(AbilitySpawnFxTime.OnPrecastStart, Resource.Sfx.PreStart_Earth),
                Step_sfx(AbilitySpawnFxTime.OnStart, Resource.Sfx.Start_Earth),
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, new ContextActionRemoveBuffAll { m_Buff = foeThrowBuff })
                ).TargetEnemy(CastAnimationStyle.Kineticist);

            return ability;
        }

        #endregion

        #endregion

        #region Wild Talents

        private static void CreateAetherWildTalents(BlueprintProgression first_prog, BlueprintProgression kinetic_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintFeature tb_feature, BlueprintFeature fw_feature)
        {
            AddToSkilledKineticist(first_prog);
            AddToKineticHealer(first_prog, second_prog, third_prog, kinetic_prog);
            AddToExpandedDefense(fw_feature);
            var invis = CreateTelekineticInvisibility(first_prog, second_prog, third_prog, kinetic_prog);
            var tf_feat = CreateTelekineticFinesse(first_prog, second_prog, third_prog, kinetic_prog);
            var maneuvers = CreateTelekineticManeuvers(first_prog, second_prog, third_prog, kinetic_prog, tf_feat);
            var touchsite = CreateTouchsiteReactive(first_prog, second_prog, third_prog, kinetic_prog);
            CreateSelfTelekinesis(first_prog, second_prog, third_prog, kinetic_prog, out var st_lesser_feat, out var st_greater_feat);
            var spell_deflection = CreateSpellDeflection(first_prog, second_prog, third_prog, kinetic_prog);
            CreateWildTalentBonusFeatAether(first_prog, second_prog, third_prog, kinetic_prog, out var wild0, out var wild1, out var wild2, out var wild3);

            try
            {
                var extra_wild = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("bd287f6d1c5247da9b81761cab64021c"); // DarkCodex's ExtraWildTalentFeat
                Helper.AppendAndReplace(ref extra_wild.m_AllFeatures, new List<BlueprintFeatureReference> { invis.ToRef(), tf_feat.ToRef(), maneuvers.ToRef(), touchsite.ToRef(), st_greater_feat.ToRef(), st_lesser_feat.ToRef(), spell_deflection.ToRef(), wild0.ToRef(), wild1.ToRef(), wild2.ToRef(), wild3.ToRef() });
            } catch (Exception ex)
            {
                Helper.Print($"Dark Codex not installed: {ex.Message}");
            }
        }

        private static void AddToSkilledKineticist(BlueprintProgression first_prog)
        {
            var kineticist_class = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391"); // KineticistMainClass
            var skilled_kineticist_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("56b70109d78b0444cb3ad04be3b1ee9e"); // SkilledKineticistBuff

            var buff = Helper.CreateBlueprintBuff("SkilledKineticistAetherBuff", "Skilled Kineticist", null, null, null, null);
            buff.Flags(true, true);
            buff.Stacking = StackingType.Replace;
            buff.SetComponents
                (
                Helper.CreateContextRankConfig(ContextRankBaseValueType.ClassLevel,
                    ContextRankProgression.Div2, max: 20, classes: new BlueprintCharacterClassReference[1] { kineticist_class.ToRef() }),
                Helper.CreateAddContextStatBonus(new ContextValue { ValueType = ContextValueType.Rank, Value = 0, ValueRank = AbilityRankType.Default, ValueShared = AbilitySharedValue.Damage },
                StatType.SkillThievery),
                Helper.CreateAddContextStatBonus(new ContextValue { ValueType = ContextValueType.Rank, Value = 0, ValueRank = AbilityRankType.Default, ValueShared = AbilitySharedValue.Damage },
                StatType.SkillKnowledgeWorld)
                );

            var condition = Helper.CreateContextConditionHasFact(first_prog.ToRef2());
            var conditional = Helper.CreateConditional(condition,
                ifTrue: buff.CreateContextActionApplyBuff(0, DurationRate.Rounds, false, false, false, true, true));

            var factContextAction = skilled_kineticist_buff.GetComponent<AddFactContextActions>();
            Helper.AppendAndReplace(ref factContextAction.Activated.Actions, conditional);
        }
        private static void AddToKineticHealer(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var feat = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("3ef66697-3adf-a8f4-0af6-c0679bd98ba5"); // Kinetic Healer Feature
            var feat_preq = feat.GetComponent<PrerequisiteFeaturesFromList>();
            Helper.AppendAndReplace(ref feat_preq.m_Features, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef());
        }
        private static void AddToExpandedDefense(BlueprintFeature fw_feature)
        {
            var selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("d741f298-dfae-8fc4-0b46-15aaf83b6548"); // Kineticist Expanded Defense Selection
            Helper.AppendAndReplace(ref selection.m_AllFeatures, fw_feature.ToRef());
        }
        private static BlueprintFeature CreateTelekineticInvisibility(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var invis_buff_icon = Helper.StealIcon("525f980c-b29b-c224-0b93-e953974cb325"); // Invisibility Effect Buff Icon
            var invis_buff = ResourcesLibrary.TryGetBlueprint<BlueprintBuff>("525f980c-b29b-c224-0b93-e953974cb325"); // Invisibility Effect Buff

            var ti_ability = Helper.CreateBlueprintAbility("TelekineticInvisibiltyAbility", "Telekinetic Invisibility",
                TelekineticInvisibilityDescription, null, invis_buff_icon, AbilityType.Special, UnitCommand.CommandType.Standard,
                AbilityRange.Personal);
            ti_ability.TargetSelf();
            ti_ability.SetComponents
                (
                Helper.CreateAbilityEffectRunAction
                    (
                    SavingThrowType.Unknown,
                    invis_buff.CreateContextActionApplyBuff(1, DurationRate.Hours, false, false, false, true)
                    ),
                Helper.CreateAbilityAcceptBurnOnCast(0)
                );

            var ti_feat = Helper.CreateBlueprintFeature("TelekineticInvisibilityFeature", "Telekinetic Invisibility",
                TelekineticInvisibilityDescription, null, invis_buff_icon, FeatureGroup.KineticWildTalent);
            ti_feat.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 6),
                Helper.CreateAddFacts(ti_ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, ti_feat.ToRef());
            return ti_feat;
        }
        private static BlueprintFeature CreateTelekineticFinesse(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.StealIcon("d6d68c99-6016-e1c4-e85e-cd0ee0067c29"); // Ranged Legerdemain

            var tf_ability = Helper.CreateBlueprintActivatableAbility("TelekineticFinesseToggleAbility", "Telekinetic Finesse",
                TelekineticFinesseDescription, out var tf_buff, null, icon, UnitCommand.CommandType.Free, Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivationType.Immediately,
                Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityGroup.None, false, false, false, false, false, false, false, false, false, 1);

            var mech_feature = new AddMechanicsFeature { m_Feature = MechanicsFeatureType.RangedLegerdemain };

            tf_buff.Flags(null, true, null, null);
            tf_buff.Stacking = StackingType.Replace;
            tf_buff.SetComponents
                (
                mech_feature,
                Helper.CreateAddContextStatBonus(Helper.CreateContextValue(AbilityRankType.StatBonus),StatType.SkillThievery),
                Helper.CreateContextRankConfig(ContextRankBaseValueType.ClassLevel, ContextRankProgression.Div2, AbilityRankType.StatBonus, classes: new BlueprintCharacterClassReference[] {kineticist_class})
                );

            var tf_feat = Helper.CreateBlueprintFeature("TelekineticFinesseFeature", "Telekinetic Finesse",
                TelekineticFinesseDescription, null, icon, FeatureGroup.KineticWildTalent);
            tf_feat.HideInCharacterSheetAndLevelUp = true;
            tf_feat.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 1),
                Helper.CreateAddFacts(tf_ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, tf_feat.ToRef());
            return tf_feat;
        }
        
        #region Telekinetic Maneuvers
        private static BlueprintFeature CreateTelekineticManeuvers(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog,  BlueprintFeature tf_feat)
        {
            var variant_trip = CreateVariant_TM_Trip();
            var variant_disarm = CreateVariant_TM_Disarm();
            var variant_bullRush = CreateVariant_TM_BullRush();
            var variant_dt_blind = CreateVariant_TM_DirtyTrick_Blind(tf_feat);
            var variant_dt_entangle = CreateVariant_TM_DirtyTrick_Entangle(tf_feat);
            var variant_dt_sickened = CreateVariant_TM_DirtyTrick_Sickened(tf_feat);
            var variant_pull = CreateVariant_TM_Pull(); ;
            var feature = CreateTeleKineticManeuversFeature(first_prog, second_prog, third_prog, kinetic_prog, variant_trip, variant_disarm, variant_bullRush, variant_dt_blind, variant_dt_entangle, variant_dt_sickened, variant_pull);
            return feature;
        }

        #region tm_variants
        private static BlueprintAbility CreateVariant_TM_Trip()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("6fd05c4e-cfeb-d6f4-d873-325de442fc17");
            var icon = Helper.StealIcon("6fd05c4e-cfeb-d6f4-d873-325de442fc17");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.Trip,
                IgnoreConcealment = false,
                ReplaceStat = true,
                UseKineticistMainStat = true
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversTripAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_Disarm()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("45d94c6d-b453-cfc4-a9b9-9b72d6afe6f6");
            var icon = Helper.StealIcon("45d94c6d-b453-cfc4-a9b9-9b72d6afe6f6");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.Disarm,
                IgnoreConcealment = false,
                ReplaceStat = true,
                UseKineticistMainStat = true
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversDisarmAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                run_action
                );

            return ability;
        }
        // Grapple doesn't appear to be an action in game
        private static BlueprintAbility CreateVariant_TM_Grapple()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("6fd05c4e-cfeb-d6f4-d873-325de442fc17");
            var icon = Helper.StealIcon("6fd05c4e-cfeb-d6f4-d873-325de442fc17");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.Trip,
                IgnoreConcealment = false,
                ReplaceStat = true,
                UseKineticistMainStat = true
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversGrappleAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_BullRush()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("7ab6f70c-996f-e9b4-597b-8332f0a3af5f");
            var icon = Helper.StealIcon("7ab6f70c-996f-e9b4-597b-8332f0a3af5f");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.BullRush,
                IgnoreConcealment = false,
                ReplaceStat = true,
                UseKineticistMainStat = true
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversBullRushAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_DirtyTrick_Blind(BlueprintFeature tf_feat)
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("8b736419-3036-a8d4-a803-08fbe16c8187");
            var icon = Helper.StealIcon("8b736419-3036-a8d4-a803-08fbe16c8187");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.DirtyTrickBlind,
                IgnoreConcealment = false,
                ReplaceStat = true,
                NewStat = StatType.Dexterity
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversDirtyTrickBlindAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact(tf_feat.ToRef2()),
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_DirtyTrick_Entangle(BlueprintFeature tf_feat)
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("5f22daa9-460c-5844-992b-f751e1e8eb78");
            var icon = Helper.StealIcon("5f22daa9-460c-5844-992b-f751e1e8eb78");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.DirtyTrickEntangle,
                IgnoreConcealment = false,
                ReplaceStat = true,
                NewStat = StatType.Dexterity
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversDirtyTrickEntangleAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact(tf_feat.ToRef2()),
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_DirtyTrick_Sickened(BlueprintFeature tf_feat)
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("4921b86e-e42c-0b54-e87a-2f9b20521ab9");
            var icon = Helper.StealIcon("4921b86e-e42c-0b54-e87a-2f9b20521ab9");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.DirtyTrickSickened,
                IgnoreConcealment = false,
                ReplaceStat = true,
                NewStat = StatType.Dexterity
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversDirtyTrickSickenedAction", parent.m_DisplayName,
                parent.Description, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                Helper.CreateAbilityShowIfCasterHasFact(tf_feat.ToRef2()),
                run_action
                );

            return ability;
        }
        private static BlueprintAbility CreateVariant_TM_Pull()
        {
            var parent = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("d131394d-9d8e-f384-2984-06cfa45bb7b7");
            var icon = Helper.StealIcon("d131394d-9d8e-f384-2984-06cfa45bb7b7");

            var action = new ContextActionCombatManeuver
            {
                Type = CombatManeuver.Pull,
                IgnoreConcealment = false,
                ReplaceStat = true,
                UseKineticistMainStat = true
            };
            var run_action = Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, action);

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversPullAction", "Pull",
                TelekineticManeuversPullDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Long);
            ability.TargetEnemy(CastAnimationStyle.Kineticist);
            ability.SpellResistance = true;
            ability.Hidden = false;
            ability.NeedEquipWeapons = false;
            ability.EffectOnAlly = AbilityEffectOnUnit.None;
            ability.EffectOnEnemy = AbilityEffectOnUnit.Harmful;
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionSpecialAttack;
            ability.m_TargetMapObjects = false;
            ability.AvailableMetamagic = Metamagic.Quicken | Metamagic.Heighten | Metamagic.Reach;

            ability.SetComponents
                (
                run_action
                );

            return ability;
        }
        #endregion

        private static BlueprintFeature CreateTeleKineticManeuversFeature(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog, params BlueprintAbility[] variants)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class

            var icon = Helper.CreateSprite("telekineticManeuvers.png");

            var ability = Helper.CreateBlueprintAbility("TelekineticManeuversAbility", "Telekinetic Maneuvers", 
                TelekineticManeuversDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard,
                AbilityRange.Long, null, null);

            foreach (var v in variants)
            {
                Helper.AddToAbilityVariants(ability, v);
            }

            var feature = Helper.CreateBlueprintFeature("TelekineticManeuversFeature", "Telekinetic Maneuvers",
                TelekineticManeuversDescription, null, icon, FeatureGroup.KineticWildTalent);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 8),
                Helper.CreateAddFacts(ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }
        
        #endregion

        private static BlueprintFeature CreateTouchsiteReactive(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.CreateSprite("touchsite.png");

            var ignore_flatFoot = new FlatFootedIgnore
            {
                Type = FlatFootedIgnoreType.UncannyDodge
            };
            var condition = new AddCondition
            {
                Condition = UnitCondition.AttackOfOpportunityBeforeInitiative
            };

            var feature = Helper.CreateBlueprintFeature("TouchsiteReactive", "Touchsite, Reactive",
                TouchsiteReactiveDescription, null, icon, FeatureGroup.KineticWildTalent);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 10),
                ignore_flatFoot,
                condition
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }
        
        #region Self Telekinesis
        private static void CreateSelfTelekinesis(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog, out BlueprintFeature lesser_feat, out BlueprintFeature greater_feat)
        {
            lesser_feat = CreateSelfTelekinesisLesser(first_prog, second_prog, third_prog, kinetic_prog);
            greater_feat = CreateSelfTelekinesisGreater(first_prog, second_prog, third_prog, kinetic_prog, lesser_feat);
        }

        private static BlueprintFeature CreateSelfTelekinesisLesser(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.StealIcon("e4979934-bdb3-9d84-2b28-bee614606823"); // Buff Wings Mutagen

            var ac_bonus = new ACBonusAgainstAttacks
            {
                AgainstMeleeOnly = true,
                AgainstRangedOnly = false,
                OnlySneakAttack = false,
                NotTouch = false,
                IsTouch = false,
                OnlyAttacksOfOpportunity = false,
                Value = Helper.CreateContextValue(0),
                ArmorClassBonus = 3,
                Descriptor = ModifierDescriptor.Dodge,
                CheckArmorCategory = false,
                NotArmorCategory = null,
                NoShield = false
            };
            var no_difficultTerrain = new AddConditionImmunity
            {
                Condition = UnitCondition.DifficultTerrain
            };

            var buff = Helper.CreateBlueprintBuff("SelfTelekinesisBuff", "Self Telekinesis",
                SelfTelekinesisDescription, null, icon);
            buff.Flags(null, true, null, null);
            buff.Stacking = StackingType.Replace;
            buff.SetComponents
                (
                ac_bonus,
                no_difficultTerrain
                );

            var ability = Helper.CreateBlueprintAbility("SelfTelekinesisAbility", "Self Telekinesis",
                SelfTelekinesisDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Move, AbilityRange.Personal);
            ability.TargetSelf(CastAnimationStyle.Kineticist);
            ability.AnimationStyle = Kingmaker.View.Animation.CastAnimationStyle.CastActionOmni;
            ability.SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, buff.CreateContextActionApplyBuff(1, DurationRate.Rounds, false, false, true, false, false))
                );

            var feature = Helper.CreateBlueprintFeature("SelfTelekinesisFeature", "Self Telekinesis", 
                SelfTelekinesisDescription, null, icon, FeatureGroup.KineticWildTalent);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 6),
                Helper.CreateAddFacts(ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, feature.ToRef());

            return feature;
        }

        private static BlueprintFeature CreateSelfTelekinesisGreater(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog, BlueprintFeature st_lesser_feat)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.StealIcon("e4979934-bdb3-9d84-2b28-bee614606823"); // Buff Wings Mutagen

            var ac_bonus = new ACBonusAgainstAttacks
            {
                AgainstMeleeOnly = true,
                AgainstRangedOnly = false,
                OnlySneakAttack = false,
                NotTouch = false,
                IsTouch = false,
                OnlyAttacksOfOpportunity = false,
                Value = Helper.CreateContextValue(0),
                ArmorClassBonus = 3,
                Descriptor = ModifierDescriptor.Dodge,
                CheckArmorCategory = false,
                NotArmorCategory = null,
                NoShield = false
            };
            var no_difficultTerrain = new AddConditionImmunity
            {
                Condition = UnitCondition.DifficultTerrain
            };

            var ability = Helper.CreateBlueprintActivatableAbility("SelfTelekinesisGreaterAbility", "Self Telekinesis, Greater",
                SelfTelekinesisGreaterDescription, out var buff, null, icon, UnitCommand.CommandType.Move, Kingmaker.UnitLogic.ActivatableAbilities.AbilityActivationType.Immediately,
                Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityGroup.None, false, false, false, false, false, false, true, false, false, 1);

            buff.Flags(false, false, null, null);
            buff.Stacking = StackingType.Replace;
            buff.SetComponents
                (
                ac_bonus,
                no_difficultTerrain
                );

            var remove_lesser = new RemoveFeatureOnApply
            {
                m_Feature = st_lesser_feat.ToRef2()
            };
            var feature = Helper.CreateBlueprintFeature("SelfTelekinesisGreaterFeature", "Self Telekinesis, Greater",
                SelfTelekinesisGreaterDescription, null, icon, FeatureGroup.KineticWildTalent);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteFeature(st_lesser_feat.ToRef()),
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 10),
                remove_lesser,
                Helper.CreateAddFacts(ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, feature.ToRef());

            st_lesser_feat.AddComponents(Helper.CreatePrerequisiteNoFeature(feature.ToRef()));

            return feature;
        }
        
        #endregion

        private static BlueprintFeature CreateSpellDeflection(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var kineticist_class_ref = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.StealIcon("50a77710-a7c4-9144-99d0-254e76a808e5"); // Spell Resistance Buff

            var add_sr = new AddSpellResistance
            {
                AddCR = false,
                AllSpellResistancePenaltyDoNotUse = false,
                Value = new ContextValue
                {
                    ValueType = ContextValueType.Shared,
                    ValueRank = AbilityRankType.Default,
                    ValueShared = AbilitySharedValue.Damage,
                    Value = 1
                }
            };

            var classlvl_value_getter = new ClassLevelGetter()
            {
                ClassRef = kineticist_class_ref
            };
            var property = Helper.CreateBlueprintUnitProperty("SpellDeflectionProperty")
                .SetComponents
                (
                classlvl_value_getter
                );
            property.BaseValue = 1;
            property.OperationOnComponents = MathOperation.Sum;

            var value = new ContextCalculateSharedValue 
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.CasterCustomProperty,
                        m_CustomProperty = property.ToRef()
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Simple,
                        Value = 9
                    }
                }
            };

            var buff = Helper.CreateBlueprintBuff("SpellDeflectionBuff", "Spell Deflection",
                SpellDeflectionDescription, null, icon, null);
            buff.Stacking = StackingType.Replace;
            buff.Flags(null, false, null, null);
            buff.SetComponents
                (
                add_sr, value
                );

            var variant_instant = Helper.CreateBlueprintAbility("SpellDeflectionAbilityInstant", "Spell Deflection",
                SpellDeflectionDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Personal);
            variant_instant.SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, 
                    buff.CreateContextActionApplyBuff(1, DurationRate.Rounds, false, false, true, false, false)),
                Helper.CreateAbilityAcceptBurnOnCast(0)
                );

            var duration_value = new ContextDurationValue
            {
                Rate = DurationRate.TenMinutes,
                DiceType = DiceType.One,
                DiceCountValue = Helper.CreateContextValue(0),
                BonusValue = new ContextValue
                {
                    m_CustomProperty = property.ToRef(),
                    Value = 1,
                    ValueRank = AbilityRankType.Default,
                    ValueShared = AbilitySharedValue.Damage,
                    ValueType = ContextValueType.CasterCustomProperty
                }
            };

            var variant_prolonged = Helper.CreateBlueprintAbility("SpellDeflectionAbilityProlonged", "Spell Deflection Extended",
                SpellDeflectionDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Personal);
            variant_prolonged.SetComponents
                (
                Helper.CreateAbilityEffectRunAction(SavingThrowType.Unknown, 
                    buff.CreateContextActionApplyBuff(duration_value, false, true, false, false)),
                Helper.CreateAbilityAcceptBurnOnCast(1)
                );

            var ability = Helper.CreateBlueprintAbility("SpellDeflectionAbility", "Spell Deflection",
                SpellDeflectionDescription, null, icon, AbilityType.SpellLike, UnitCommand.CommandType.Standard, AbilityRange.Personal);

            ability.AddToAbilityVariants(variant_instant, variant_prolonged);

            var feature = Helper.CreateBlueprintFeature("SpellDeflectionFeature", "Spell Deflection",
                SpellDeflectionDescription, null, icon, FeatureGroup.KineticWildTalent);
            feature.SetComponents
                (
                Helper.CreatePrerequisiteFeaturesFromList(true, first_prog.ToRef(), second_prog.ToRef(), third_prog.ToRef(), kinetic_prog.ToRef()),
                Helper.CreatePrerequisiteClassLevel(kineticist_class, 14),
                Helper.CreateAddFacts(ability.ToRef2())
                );

            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, feature.ToRef());
            return feature;
        }
        private static void CreateSuffocationWildTalent(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection
            var kineticist_class = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            //var kineticist_class_ref = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9-ec1a-d924-d889-272429eb8391"); // Kineticist Base Class
            var icon = Helper.StealIcon("b3c6cb76-d5b1-1cf4-c831-4d7b1c7b9b8b"); // Choking Bomb feature
        }

        private static void CreateWildTalentBonusFeatAether(BlueprintProgression first_prog, BlueprintProgression second_prog, BlueprintProgression third_prog, BlueprintProgression kinetic_prog, out BlueprintFeatureSelection wild_0, out BlueprintFeatureSelection wild_1, out BlueprintFeatureSelection wild_2, out BlueprintFeatureSelection wild_3)
        {
            var wild_talent_selection = ResourcesLibrary.TryGetBlueprint<BlueprintFeatureSelection>("5c883ae0-cd6d-7d54-48b7-a420f51f8459"); // Kineticist Wild Talent Selection

            var spell_pen = Helper.ToRef<BlueprintFeatureReference>("ee7dc126939e4d9438357fbd5980d459"); // SpellPenetration
            var spell_pen_greater = Helper.ToRef<BlueprintFeatureReference>("1978c3f91cfbbc24b9c9b0d017f4beec"); // GreaterSpellPenetration
            var precise_shot = Helper.ToRef<BlueprintFeatureReference>("8f3d1e6b4be006f4d896081f2f889665"); // PreciseShot
            var trip = Helper.ToRef<BlueprintFeatureReference>("0f15c6f70d8fb2b49aa6cc24239cc5fa"); // ImprovedTrip
            var trip_greater = Helper.ToRef<BlueprintFeatureReference>("4cc71ae82bdd85b40b3cfe6697bb7949"); // SpellPenetration

            wild_0 = Helper.CreateBlueprintFeatureSelection("WildTalentBonusFeatAether", aether_wild_talent_name,
                aether_wild_talent_description, null, null, FeatureGroup.KineticWildTalent, SelectionMode.Default);
            wild_0.SetComponents
                (
                Helper.CreatePrerequisiteFeature(first_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(second_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(third_prog.ToRef(), true),
                Helper.CreatePrerequisiteNoFeature(trip, false),
                Helper.CreatePrerequisiteNoFeature(spell_pen, false),
                Helper.CreatePrerequisiteFeature(kinetic_prog.ToRef(), true)
                );
            wild_0.IgnorePrerequisites = true;
            Helper.AppendAndReplace(ref wild_0.m_AllFeatures, spell_pen, precise_shot, trip);

            wild_1 = Helper.CreateBlueprintFeatureSelection("WildTalentBonusFeatAether1", aether_wild_talent_name,
                aether_wild_talent_description, null, null, FeatureGroup.KineticWildTalent, SelectionMode.Default);
            wild_1.SetComponents
                (
                Helper.CreatePrerequisiteFeature(first_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(second_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(third_prog.ToRef(), true),
                Helper.CreatePrerequisiteNoFeature(trip, false),
                Helper.CreatePrerequisiteFeature(spell_pen, false),
                Helper.CreatePrerequisiteFeature(kinetic_prog.ToRef(), true)
                );
            wild_1.IgnorePrerequisites = true;
            Helper.AppendAndReplace(ref wild_1.m_AllFeatures, spell_pen_greater, precise_shot, trip);

            wild_2 = Helper.CreateBlueprintFeatureSelection("WildTalentBonusFeatAether2", aether_wild_talent_name,
                aether_wild_talent_description, null, null, FeatureGroup.KineticWildTalent, SelectionMode.Default);
            wild_2.SetComponents
                (
                Helper.CreatePrerequisiteFeature(first_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(second_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(third_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(trip, false),
                Helper.CreatePrerequisiteNoFeature(spell_pen, false),
                Helper.CreatePrerequisiteFeature(kinetic_prog.ToRef(), true)
                );
            wild_2.IgnorePrerequisites = true;
            Helper.AppendAndReplace(ref wild_2.m_AllFeatures, spell_pen, precise_shot, trip_greater);

            wild_3 = Helper.CreateBlueprintFeatureSelection("WildTalentBonusFeatAether3", aether_wild_talent_name,
                aether_wild_talent_description, null, null, FeatureGroup.KineticWildTalent, SelectionMode.Default);
            wild_3.SetComponents
                (
                Helper.CreatePrerequisiteFeature(first_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(second_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(third_prog.ToRef(), true),
                Helper.CreatePrerequisiteFeature(trip, false),
                Helper.CreatePrerequisiteFeature(spell_pen, false),
                new PrerequisiteSelectionPossible
                {
                    m_ThisFeature = wild_3.ToRef3()
                },
                Helper.CreatePrerequisiteFeature(kinetic_prog.ToRef(), true)
                );
            wild_3.IgnorePrerequisites = true;
            Helper.AppendAndReplace(ref wild_3.m_AllFeatures, spell_pen_greater, precise_shot, trip_greater);


            Helper.AppendAndReplace(ref wild_talent_selection.m_AllFeatures, wild_0.ToRef(), wild_1.ToRef(), wild_2.ToRef(), wild_3.ToRef());
        }

        #endregion

        #region Area Effects

        public static BlueprintAbilityAreaEffect CreateTelekineticWallEffect()
        {
            var kineticist_class = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391"); // KineticistMainClass
            var kineticist_main_stat_property = "f897845bbbc008d4f9c1c4a03e22357a".ToRef<BlueprintUnitPropertyReference>(); // KineticistMainStatProperty
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c"); // KineticBlastFeature
            var wall_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c684335918896ce4ab13e96cec929796"); // WallInfusion
            var unique = new UniqueAreaEffect { m_Feature = wall_infusion.ToRef2() };
            var prefab = new PrefabLink { AssetId = "4ffc8d2162a215e44a1a728752b762eb" }; // AirBlastWallEffect PrefabLink

            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, Helper.CreateContextValue(AbilityRankType.DamageDice), Helper.CreateContextValue(AbilitySharedValue.Damage));

            var context_dealDamage = Helper.CreateContextActionDealDamage(PhysicalDamageForm.Bludgeoning | PhysicalDamageForm.Piercing | PhysicalDamageForm.Slashing,
                dice, false, false, false, true, false, AbilitySharedValue.Damage);
            ActionList action_list = new() { Actions = new GameAction[1] { context_dealDamage } };

            var area_effect = Helper.CreateBlueprintAbilityAreaEffect("WallTelekineticBlastArea", null, true, true,
                AreaEffectShape.Wall, new Feet { m_Value = 60 },
                prefab, unitEnter: action_list);
            area_effect.m_Tags = AreaEffectTags.DestroyableInCutscene;
            area_effect.IgnoreSleepingUnits = false;
            area_effect.AffectDead = false;
            area_effect.AggroEnemies = true;
            area_effect.AffectEnemies = true;
            area_effect.SpellResistance = false;

            var context1 = Helper.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, stat: StatType.Constitution, 
                type: AbilityRankType.DamageBonus, customProperty: kineticist_main_stat_property, min: 0, max: 20);
            var context2 = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, stat: StatType.Constitution,
                type: AbilityRankType.DamageDice, customProperty: kineticist_main_stat_property, min: 0, max: 20,
                feature: kinetic_blast_feature.ToRef());

            var calc_shared = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageBonus,
                        ValueShared = AbilitySharedValue.Damage
                    }
                }
            };

            var calc_ability_params = new ContextCalculateAbilityParamsBasedOnClass
            {
                UseKineticistMainStat = true,
                StatType = StatType.Charisma,
                m_CharacterClass = kineticist_class.ToRef()
            };

            area_effect.AddComponents(unique, context1, context2, calc_shared, calc_ability_params);

            return area_effect;
        }

        public static BlueprintAbilityAreaEffect CreateForceWallEffect()
        {
            var kineticist_class = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>("42a455d9ec1ad924d889272429eb8391"); // KineticistMainClass
            var kineticist_main_stat_property = "f897845bbbc008d4f9c1c4a03e22357a".ToRef<BlueprintUnitPropertyReference>(); // KineticistMainStatProperty
            var kinetic_blast_feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("93efbde2764b5504e98e6824cab3d27c"); // KineticBlastFeature
            var wall_infusion = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c684335918896ce4ab13e96cec929796"); // WallInfusion
            var unique = new UniqueAreaEffect { m_Feature = wall_infusion.ToRef2() };
            var prefab = new PrefabLink { AssetId = "4ffc8d2162a215e44a1a728752b762eb" }; // AirBlastWallEffect PrefabLink

            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, Helper.CreateContextValue(AbilityRankType.DamageDice), Helper.CreateContextValue(AbilityRankType.DamageBonus));

            var context_dealDamage = Helper.CreateContextActionDealDamageForce(DamageEnergyType.Fire,
                dice, false, false, false, false, false, AbilitySharedValue.Damage);
            ActionList action_list = new() { Actions = new GameAction[1] { context_dealDamage } };

            var area_effect = Helper.CreateBlueprintAbilityAreaEffect("WallForceBlastArea", null, true, true,
                AreaEffectShape.Wall, new Feet { m_Value = 60 },
                prefab, unitEnter: action_list);
            area_effect.m_Tags = AreaEffectTags.DestroyableInCutscene;
            area_effect.IgnoreSleepingUnits = false;
            area_effect.AffectDead = false;
            area_effect.AggroEnemies = true;
            area_effect.AffectEnemies = true;
            area_effect.SpellResistance = true;

            var context1 = Helper.CreateContextRankConfig(ContextRankBaseValueType.CustomProperty, stat: StatType.Constitution,
                type: AbilityRankType.DamageBonus, customProperty: kineticist_main_stat_property, min: 0, max: 20);
            var context2 = Helper.CreateContextRankConfig(ContextRankBaseValueType.FeatureRank, stat: StatType.Constitution,
                type: AbilityRankType.DamageDice, min: 0, max: 20, feature: kinetic_blast_feature.ToRef());

            var calc_shared = new ContextCalculateSharedValue
            {
                ValueType = AbilitySharedValue.Damage,
                Modifier = 1.0,
                Value = new ContextDiceValue
                {
                    DiceType = DiceType.One,
                    DiceCountValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageDice,
                        ValueShared = AbilitySharedValue.Damage
                    },
                    BonusValue = new ContextValue
                    {
                        ValueType = ContextValueType.Rank,
                        Value = 0,
                        ValueRank = AbilityRankType.DamageBonus,
                        ValueShared = AbilitySharedValue.Damage
                    }
                }
            };

            var calc_ability_params = new ContextCalculateAbilityParamsBasedOnClass
            {
                UseKineticistMainStat = true,
                StatType = StatType.Charisma,
                m_CharacterClass = kineticist_class.ToRef()
            };

            area_effect.AddComponents(unique, context1, context2, calc_shared, calc_ability_params);

            return area_effect;
        }

        #endregion

        #region Helper

        /// <summary>
        /// 1) make BlueprintAbility
        /// 2) set SpellResistance
        /// 3) make components with helpers (step1 to 9)
        /// 4) set m_Parent to XBlastBase with Helper.AddToAbilityVariants
        /// Logic for dealing damage. Will make a composite blast, if both p and e are set. How much damage is dealt is defined in step 2.
        /// </summary>
        public static AbilityEffectRunAction Step1_run_damage(out ActionList actions, PhysicalDamageForm p = 0, DamageEnergyType e = (DamageEnergyType)255, SavingThrowType save = SavingThrowType.Unknown, bool isAOE = false, bool half = false)
        {
            ContextDiceValue dice = Helper.CreateContextDiceValue(DiceType.D6, AbilityRankType.DamageDice, AbilityRankType.DamageBonus);

            List<ContextAction> list = new(2);

            bool isComposite = p != 0 && e != (DamageEnergyType)255;

            if (p != 0)
                list.Add(Helper.CreateContextActionDealDamage(p, dice, isAOE, isAOE, false, half, isComposite, AbilitySharedValue.DurationSecond, writeShare: isComposite));
            if (e != (DamageEnergyType)255)
                list.Add(Helper.CreateContextActionDealDamage(e, dice, isAOE, isAOE, false, half, isComposite, AbilitySharedValue.DurationSecond, readShare: isComposite));

            var runaction = Helper.CreateAbilityEffectRunAction(save, list.ToArray());
            actions = runaction.Actions;
            return runaction;
        }

        /// <summary>
        /// Defines damage dice. Set twice for composite blasts that are pure energy or pure physical. You shouldn't need half at all.
        /// </summary>
        public static ContextRankConfig Step2_rank_dice(bool twice = false, bool half = false)
        {
            var progression = ContextRankProgression.AsIs;
            if (half) progression = ContextRankProgression.Div2;
            if (twice) progression = ContextRankProgression.MultiplyByModifier;

            var rankdice = Helper.CreateContextRankConfig(
                baseValueType: ContextRankBaseValueType.FeatureRank,
                type: AbilityRankType.DamageDice,
                progression: progression,
                stepLevel: twice ? 2 : 0,
                feature: "93efbde2764b5504e98e6824cab3d27c".ToRef<BlueprintFeatureReference>()); //KineticBlastFeature
            return rankdice;
        }

        /// <summary>
        /// Defines bonus damage. Set half_bonus for energy blasts.
        /// </summary>
        public static ContextRankConfig Step3_rank_bonus(bool half_bonus = false)
        {
            var rankdice = Helper.CreateContextRankConfig(
                baseValueType: ContextRankBaseValueType.CustomProperty,
                progression: half_bonus ? ContextRankProgression.Div2 : ContextRankProgression.AsIs,
                type: AbilityRankType.DamageBonus,
                stat: StatType.Constitution,
                customProperty: "f897845bbbc008d4f9c1c4a03e22357a".ToRef<BlueprintUnitPropertyReference>()); //KineticistMainStatProperty
            return rankdice;
        }

        /// <summary>
        /// Simply makes the DC dex based.
        /// </summary>
        public static ContextCalculateAbilityParamsBasedOnClass Step4_dc()
        {
            var dc = new ContextCalculateAbilityParamsBasedOnClass();
            dc.StatType = StatType.Dexterity;
            dc.m_CharacterClass = Helper.ToRef<BlueprintCharacterClassReference>("42a455d9ec1ad924d889272429eb8391"); //KineticistClass
            return dc;
        }

        /// <summary>
        /// Creates damage tooltip from the run-action. Defines burn cost. Blast cost is 0, except for composite blasts which is 2. Talent is not used.
        /// </summary>
        public static AbilityKineticist Step5_burn(ActionList actions, int infusion = 0, int blast = 0, int talent = 0)
        {
            var comp = new AbilityKineticist();
            comp.InfusionBurnCost = infusion;
            comp.BlastBurnCost = blast;
            comp.WildTalentBurnCost = talent;

            if (actions?.Actions == null)
                return comp;

            for (int i = 0; i < actions.Actions.Length; i++)
            {
                if (actions.Actions[i] is not ContextActionDealDamage action)
                    continue;
                comp.CachedDamageInfo.Add(new AbilityKineticist.DamageInfo() { Value = action.Value, Type = action.DamageType, Half = action.Half });
            }
            return comp;
        }

        /// <summary>
        /// Required feat for this ability to show up.
        /// </summary>
        public static AbilityShowIfCasterHasFact Step6_feat(BlueprintFeature fact)
        {
            return Helper.CreateAbilityShowIfCasterHasFact(fact.ToRef2());
        }

        /// <summary>
        /// Defines projectile.
        /// </summary>
        public static AbilityDeliverProjectile Step7_projectile(string projectile_guid, bool isPhysical, AbilityProjectileType type, float length, float width)
        {
            string weapon = isPhysical ? "65951e1195848844b8ab8f46d942f6e8" : "4d3265a5b9302ee4cab9c07adddb253f"; //KineticBlastPhysicalWeapon //KineticBlastEnergyWeapon
            //KineticBlastPhysicalBlade b05a206f6c1133a469b2f7e30dc970ef
            //KineticBlastEnergyBlade a15b2fb1d5dc4f247882a7148d50afb0

            var projectile = Helper.CreateAbilityDeliverProjectile(
                projectile_guid.ToRef<BlueprintProjectileReference>(),
                type,
                weapon.ToRef<BlueprintItemWeaponReference>(),
                length.Feet(),
                width.Feet());
            return projectile;
        }

        /// <summary>
        /// Alternative projectile. Requires attack roll, if weapon is not null.
        /// </summary>
        public static AbilityDeliverChainAttack Step7b_chain_projectile(string projectile_guid, [CanBeNull] BlueprintItemWeaponReference weapon, float delay = 0f)
        {
            var result = new AbilityDeliverChainAttack();
            result.TargetsCount = Helper.CreateContextValue(AbilityRankType.DamageDice);
            result.TargetType = TargetType.Enemy;
            result.Weapon = weapon;
            result.Projectile = projectile_guid.ToRef<BlueprintProjectileReference>();
            result.DelayBetweenChain = delay;
            return result;
        }

        /// <summary>
        /// Alternative projectile. Requires attack roll, if weapon is not null.
        /// </summary>
        public static AbilityDeliverProjectile Step7c_simple_projectile(string projectile_guid, bool isPhysical)
        {
            string weapon = isPhysical ? "65951e1195848844b8ab8f46d942f6e8" : "4d3265a5b9302ee4cab9c07adddb253f"; //KineticBlastPhysicalWeapon //KineticBlastEnergyWeapon
            //KineticBlastPhysicalBlade b05a206f6c1133a469b2f7e30dc970ef
            //KineticBlastEnergyBlade a15b2fb1d5dc4f247882a7148d50afb0

            var result = new AbilityDeliverProjectile();
            result.m_Projectiles = projectile_guid.ToRef<BlueprintProjectileReference>().ObjToArray();
            result.Type = AbilityProjectileType.Simple;
            result.m_Weapon = weapon.ToRef<BlueprintItemWeaponReference>();
            result.NeedAttackRoll = true;
            return result;
        }


        /// <summary>
        /// Element descriptor for energy blasts.
        /// </summary>
        public static SpellDescriptorComponent Step8_spell_description(SpellDescriptor descriptor)
        {
            return new SpellDescriptorComponent
            {
                Descriptor = descriptor
            };
        }

        // <summary>
        // This is identical for all blasts or is missing completely. It seems to me as if it not used and a leftover.
        // </summary>
        //public static ContextCalculateSharedValue step9_shared_value()
        //{
        //    return Helper.CreateContextCalculateSharedValue();
        //}

        /// <summary>
        /// Defines sfx for casting.
        /// Use either use either OnPrecastStart or OnStart for time.
        /// </summary>
        public static AbilitySpawnFx Step_sfx(AbilitySpawnFxTime time, string sfx_guid)
        {
            var sfx = new AbilitySpawnFx();
            sfx.Time = time;
            sfx.PrefabLink = new PrefabLink() { AssetId = sfx_guid };
            return sfx;
        }

        public static BlueprintBuff ExpandSubstance(BlueprintBuff buff, BlueprintAbilityReference baseBlast)
        {
            Helper.AppendAndReplace(ref buff.GetComponent<AddKineticistInfusionDamageTrigger>().m_AbilityList, baseBlast);
            Helper.AppendAndReplace(ref buff.GetComponent<AddKineticistBurnModifier>().m_AppliableTo, baseBlast);
            return buff;
        }

        #endregion
    }
}
