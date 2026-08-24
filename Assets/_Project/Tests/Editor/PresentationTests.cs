using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.UI;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Step 6 of the pass, or the half of it a test can hold.
    ///
    /// Whether the Battlemage actually renders purple is a question for eyes — USS
    /// resolving a class to a colour is not something an EditMode test can see. What a
    /// test *can* pin down is that every rarity has a class at all, that no two share
    /// one, and that the two gates a player has to tell apart produce different
    /// sentences. Those are the parts that would break silently.
    /// </summary>
    public sealed class PresentationTests
    {
        [Test]
        public void EveryRarityHasItsOwnStyleClass()
        {
            System.Collections.Generic.HashSet<string> classes = new System.Collections.Generic.HashSet<string>();

            foreach (Rarity rarity in System.Enum.GetValues(typeof(Rarity)))
            {
                string styleClass = Format.RarityClass(rarity);

                Assert.That(string.IsNullOrWhiteSpace(styleClass), Is.False,
                    $"{rarity} has no style class, so it will render in the default text colour.");

                Assert.That(classes.Add(styleClass), Is.True,
                    $"{rarity} shares the class '{styleClass}' with another band, so two rarities look alike.");
            }
        }

        [Test]
        public void TheTwoRecruitmentLocksReadDifferently()
        {
            string tierLocked = Outcomes.Describe(RecruitOutcome.TierLocked, "Dragonsworn Champion");
            string rarityLocked = Outcomes.Describe(RecruitOutcome.RarityLocked, "Dragonsworn Champion");

            Assert.That(string.IsNullOrWhiteSpace(tierLocked), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(rarityLocked), Is.False);

            Assert.That(rarityLocked, Is.Not.EqualTo(tierLocked),
                "A player has to be able to tell a gate they can spend past from one they can only travel past. " +
                "Identical copy for both is the same as no explanation at all.");
        }

        [Test]
        public void EveryRefusalSaysSomething()
        {
            foreach (RecruitOutcome outcome in System.Enum.GetValues(typeof(RecruitOutcome)))
            {
                if (outcome == RecruitOutcome.Recruited)
                {
                    continue;
                }

                Assert.That(string.IsNullOrWhiteSpace(Outcomes.Describe(outcome, "Militia Recruit")), Is.False,
                    $"{outcome} has no sentence, so the button would be disabled with nothing beside it.");
            }

            foreach (UpgradeOutcome outcome in System.Enum.GetValues(typeof(UpgradeOutcome)))
            {
                if (outcome == UpgradeOutcome.Upgraded)
                {
                    continue;
                }

                Assert.That(string.IsNullOrWhiteSpace(Outcomes.Describe(outcome, "Tavern")), Is.False);
            }

            foreach (DispatchOutcome outcome in System.Enum.GetValues(typeof(DispatchOutcome)))
            {
                if (outcome == DispatchOutcome.Dispatched)
                {
                    continue;
                }

                Assert.That(string.IsNullOrWhiteSpace(Outcomes.Describe(outcome, "Sunken Crypt")), Is.False);
            }
        }

        [Test]
        public void EveryGuildStatHasAPlayerFacingName()
        {
            foreach (GuildStat stat in System.Enum.GetValues(typeof(GuildStat)))
            {
                string name = Format.StatName(stat);

                Assert.That(string.IsNullOrWhiteSpace(name), Is.False, $"{stat} has no display name.");
                Assert.That(name, Is.Not.EqualTo(stat.ToString()),
                    $"{stat} falls through to its enum name, which is how the code talks about it rather than " +
                    "how a player would.");
            }
        }
    }
}
