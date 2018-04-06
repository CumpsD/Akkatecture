﻿using System.ComponentModel;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Akkatecture.Aggregates;
using Akkatecture.TestHelpers.Aggregates;
using Akkatecture.TestHelpers.Aggregates.Commands;
using Akkatecture.TestHelpers.Aggregates.Entities;
using Akkatecture.TestHelpers.Aggregates.Events;
using Akkatecture.TestHelpers.Aggregates.Events.Signals;
using Akkatecture.TestHelpers.Akka;
using Xunit;

namespace Akkatecture.Tests.UnitTests.Aggregates
{
    [Collection("AggregateTests")]
    public class AggregateTests : TestKit
    {
        private const string Category = "Aggregates";

        public AggregateTests()
            : base(Configuration.Config)
        {
            
        }

        [Fact]
        [Category(Category)]
        public void InitialState_AfterAggregateCreation_TestCreatedEventEmitted()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestCreatedEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");
            
            var aggregateId = TestAggregateId.New;
            var command = new CreateTestCommand(aggregateId);
            aggregateManager.Tell(command);

            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestCreatedEvent>>(
                x => x.AggregateEvent.TestAggregateId.Equals(aggregateId));
        }

        [Fact]
        [Category(Category)]
        public void EventContainerMetadata_AfterAggregateCreation_TestCreatedEventEmitted()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestCreatedEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");

            var aggregateId = TestAggregateId.New;
            var command = new CreateTestCommand(aggregateId);
            aggregateManager.Tell(command);

            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestCreatedEvent>>(
                x => x.AggregateIdentity.Equals(aggregateId)
                    && x.IdentityType == typeof(TestAggregateId)
                    && x.AggregateType == typeof(TestAggregate)
                    && x.EventType == typeof(TestCreatedEvent)
                    //&& x.Metadata.EventName == "TestCreated"
                    && x.Metadata.AggregateId == aggregateId.Value
                    //&& x.Metadata.EventVersion == 1
                    && x.Metadata.AggregateSequenceNumber == 1);
        }

        [Fact]
        [Category(Category)]
        public void InitialState_AfterAggregateCreation_TestStateSignalled()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestStateSignalEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");

            var aggregateId = TestAggregateId.New;
            var command = new CreateTestCommand(aggregateId);
            var nextCommand = new PublishTestStateCommand(aggregateId);
            aggregateManager.Tell(command);
            aggregateManager.Tell(nextCommand);

            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestStateSignalEvent>>(
                x => x.AggregateEvent.LastSequenceNr == 1
                     && x.AggregateEvent.Version == 1
                     && x.AggregateEvent.State.TestCollection.Count == 0);
        }

        [Fact]
        [Category(Category)]
        public void TestCommand_AfterAggregateCreation_TestEventEmitted()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");

            var aggregateId = TestAggregateId.New;
            var command = new CreateTestCommand(aggregateId);
            var testId = TestId.New;
            var test = new Test(testId);
            var nextCommand = new AddTestCommand(aggregateId,test);
            aggregateManager.Tell(command);
            aggregateManager.Tell(nextCommand);

            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>>(
                x => x.AggregateEvent.Test.Equals(test));
        }

        [Fact]
        [Category(Category)]
        public void TestCommandTwice_AfterAggregateCreation_TestEventEmitted()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");


            var aggregateId = TestAggregateId.New;
            var command = new CreateTestCommand(aggregateId);
            var testId = TestId.New;
            var test = new Test(testId);
            var nextCommand = new AddTestCommand(aggregateId, test);
            var test2Id = TestId.New;
            var test2 = new Test(test2Id);
            var nextCommand2 = new AddTestCommand(aggregateId, test2);
            aggregateManager.Tell(command);
            aggregateManager.Tell(nextCommand);
            aggregateManager.Tell(nextCommand2);


            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>>(
                x => x.AggregateEvent.Test.Equals(test)
                     && x.AggregateSequenceNumber == 2);

            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestAddedEvent>>(
                x => x.AggregateEvent.Test.Equals(test2)
                     && x.AggregateSequenceNumber == 3);
        }

        [Fact]
        [Category(Category)]
        public void TestEventSourcing_AfterManyTests_TestStateSignalled()
        {
            var probe = CreateTestActor("probeActor");
            Sys.EventStream.Subscribe(probe, typeof(DomainEvent<TestAggregate, TestAggregateId, TestStateSignalEvent>));
            var aggregateManager = Sys.ActorOf(Props.Create(() => new TestAggregateManager()), "test-aggregatemanager");
            var aggregateId = TestAggregateId.New;

            
            var command = new CreateTestCommand(aggregateId);
            aggregateManager.Tell(command);

            for (var i = 0; i < 5; i++)
            {
                var test = new Test(TestId.New);
                var testCommand = new AddTestCommand(aggregateId, test);
                aggregateManager.Tell(testCommand);
            }
            
            var poisonCommand = new PoisonTestAggregateCommand(aggregateId);
            aggregateManager.Tell(poisonCommand);

            var reviveCommand = new PublishTestStateCommand(aggregateId);
            aggregateManager.Tell(reviveCommand);


            ExpectMsg<DomainEvent<TestAggregate, TestAggregateId, TestStateSignalEvent>>(
                x => x.AggregateEvent.LastSequenceNr == 6
                     && x.AggregateEvent.Version == 6
                     && x.AggregateEvent.State.TestCollection.Count == 5);

        }
    }
}