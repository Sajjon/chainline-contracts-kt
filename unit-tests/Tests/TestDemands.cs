﻿using Neo.VM;
using Xunit;
using Xunit.Abstractions;
using System;
using System.Linq;

namespace CLTests {
   public class TestDemands : Test {
      public TestDemands(ITestOutputHelper output) : base(output) { }

      static readonly byte[] ScriptHash = new byte[] {
         5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
         5, 4, 3, 2, 1, 5, 4, 3, 2,
         0xFF
      };

      static readonly byte[] Info = new byte[] {
         1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2,  // line - 32 bytes
         1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2,
         1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2,
         1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 1,
         0xFF
      };

      static readonly byte[] Demand = new byte[] {
         // expiry (4 byte timestamp)
         1, 0, 0, 0,
         // itemValue (100000000)
         0x00, 0xE1, 0xF5, 0x05, 0x00,
         // owner script hash
         5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
         5, 4, 3, 2, 1, 5, 4, 3, 2,
         0xFF,
         // repRequired
         2, 0,
         // itemSize
         1,
         // info
      }.Concat(Info).ToArray();

      [Fact]
      public void TestCreateDemand() {
         ExecutionEngine engine = LoadContract("HubContract");

         // private fun demand_create(cityHash: Hash160, repRequired: BigInteger, itemSize: BigInteger,
         //                            itemValue: BigInteger, info: ByteArray): Demand {

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Info);  // args[5] - info
            sb.EmitPush(100000000);  // args[4] - itemValue
            sb.EmitPush(1);  // args[3] - itemSize
            sb.EmitPush(2);  // args[2] - repRequired
            sb.EmitPush(1);  // args[1] - expiry
            sb.EmitPush(ScriptHash);  // args[0] - owner
            sb.EmitPush(6);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_create");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(Demand, result);
      }

      [Fact]
      public void TestCreateDemandValidationValueTooHigh() {
         ExecutionEngine engine = LoadContract("HubContract");

         // failure case: itemValue is too high below.

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Info);  // args[5] - info
            sb.EmitPush(550000000000);  // args[4] - itemValue
            sb.EmitPush(1);  // args[3] - itemSize
            sb.EmitPush(1);  // args[2] - repRequired
            sb.EmitPush(1);  // args[1] - expiry
            sb.EmitPush(ScriptHash);  // args[0] - owner
            sb.EmitPush(6);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_create");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(new byte[] { }, result);
      }

      [Fact]
      public void TestCreateDemandValidationItemSizeTooHigh() {
         ExecutionEngine engine = LoadContract("HubContract");

         // failure case: itemValue is too high below.

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Info);  // args[5] - info
            sb.EmitPush(100000000);  // args[4] - itemValue
            sb.EmitPush(128);  // args[3] - itemSize (max is 127)
            sb.EmitPush(1);  // args[2] - repRequired
            sb.EmitPush(1);  // args[1] - expiry
            sb.EmitPush(ScriptHash);  // args[0] - owner
            sb.EmitPush(6);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_create");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(new byte[] { }, result);
      }

      [Fact]
      public void TestGetDemandItemValue() {
         ExecutionEngine engine = LoadContract("HubContract");

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Demand);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_getItemValue");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetBigInteger();
         Assert.Equal(100000000, result);
      }

      [Fact]
      public void TestGetDemandTotalValue() {
         ExecutionEngine engine = LoadContract("HubContract");

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Demand);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_getTotalValue");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetBigInteger();
         Assert.Equal(500000000, result);  // item (1 GAS) + fee (4 GAS)
      }

      [Fact]
      public void TestGetDemandInfoBlob() {
         ExecutionEngine engine = LoadContract("HubContract");

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(Demand);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_getInfoBlob");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(Info, result);
      }

      [Fact]
      public void TestGetDemandLookupKey() {
         ExecutionEngine engine = LoadContract("HubContract");

         using (ScriptBuilder sb = new ScriptBuilder()) {
            // input is a city pair hash160
            sb.EmitPush(ScriptHash);
            sb.EmitPush(Demand);
            sb.EmitPush(2);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_getLookupKey");  // operation
            ExecuteScript(engine, sb);
         }
         var expected = new byte[] {
            1, 0, 0, 0,  // nowTime
            1, 0, 0, 0,  // expiry
            1  // STORAGE_KEY_SUFFIX_DEMAND
         }.Concat(ScriptHash).ToArray();
         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(expected, result);
      }

      [Fact]
      public void TestGetDemandStorageKey() {
         ExecutionEngine engine = LoadContract("HubContract");

         using (ScriptBuilder sb = new ScriptBuilder()) {
            // input is a city pair hash160
            sb.EmitPush(ScriptHash);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_getStorageKey");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(ScriptHash.Concat(new byte[] { 1 }).ToArray(), result);
      }

      [Fact]
      public void TestFindMatchableDemandPass() {
         ExecutionEngine engine = LoadContract("HubContract");

         var nowTime = 101;
         byte[] expiredExpiry = BitConverter.GetBytes(100).ToArray();
         byte[] futureExpiry = BitConverter.GetBytes(102).ToArray();

         // demand1 - already expired
         var demand1 = expiredExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         // demand2 - item too large
         var demand2 = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            2
            // info
         }).Concat(Info).ToArray();

         // demand3 - suitable
         var demand3 = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         var demands = demand1.Concat(demand2).Concat(demand3).ToArray();

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(nowTime);  // args[4] - nowTime
            sb.EmitPush(100);  // args[3] - expiresAfter
            sb.EmitPush(1);  // args[2] - carrySpace
            sb.EmitPush(0);  // args[1] - repRequired
            sb.EmitPush(demands);  // args[0]
            sb.EmitPush(5);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_findMatchableDemand");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(demand3, result);
      }

      [Fact]
      public void TestFindMatchableDemandWhenFirstIsMatched() {
         var nowTime = 101;
         byte[] expiredExpiry = BitConverter.GetBytes(100).ToArray();
         byte[] futureExpiry = BitConverter.GetBytes(102).ToArray();

         // demand1 - suitable but matched :(
         var demand1 = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         var demand1MatchKey = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
         }).ToArray();

         // demand2 - suitable, unmatched
         var demand2 = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000), slightly different to demand1 above
            0x01, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         ExecutionEngine engine1 = LoadContract("HubContract");
         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(new byte[] { 1 });  // args[1] - value
            sb.EmitPush(demand1MatchKey);  // args[0] - key
            sb.EmitPush(2);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_storage_put");  // operation
            ExecuteScript(engine1, sb);
         }

         ExecutionEngine engine2 = LoadContract("HubContract");
         var demands = demand1.Concat(demand2).ToArray();
         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(nowTime);  // args[4] - nowTime
            sb.EmitPush(100);  // args[3] - expiresAfter
            sb.EmitPush(1);  // args[2] - carrySpace
            sb.EmitPush(0);  // args[1] - repRequired
            sb.EmitPush(demands);  // args[0]
            sb.EmitPush(5);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_findMatchableDemand");  // operation
            ExecuteScript(engine2, sb);
         }

         var result = engine2.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(demand2, result);
      }

      [Fact]
      public void TestFindMatchableDemandUserRepCheckPass() {
         var nowTime = 101;
         byte[] expiredExpiry = BitConverter.GetBytes(100).ToArray();
         byte[] futureExpiry = BitConverter.GetBytes(102).ToArray();

         // demand - suitable
         var demand = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFD,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         // the demand owner's script hash - to get some reputation below
         var ownerScriptHash = new byte[] {
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFD,
         };

         ExecutionEngine engine1 = LoadContract("HubContract");
         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(ownerScriptHash);  // args[0]
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_wallet_incrementReputationScore");  // operation
            ExecuteScript(engine1, sb);
         }

         ExecutionEngine engine2 = LoadContract("HubContract");
         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(nowTime);  // args[4] - nowTime
            sb.EmitPush(100);  // args[3] - expiresAfter
            sb.EmitPush(1);  // args[2] - carrySpace
            sb.EmitPush(1);  // args[1] - repRequired
            sb.EmitPush(demand);  // args[0]
            sb.EmitPush(5);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_findMatchableDemand");  // operation
            ExecuteScript(engine2, sb);
         }

         var result = engine2.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(demand, result);
      }

      [Fact]
      public void TestFindMatchableDemandUserRepCheckFail() {
         var nowTime = 101;
         byte[] expiredExpiry = BitConverter.GetBytes(100).ToArray();
         byte[] futureExpiry = BitConverter.GetBytes(102).ToArray();

         // demand - suitable
         var demand = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFE,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         // the demand owner's script hash - starts out with no reputation
         var ownerScriptHash = new byte[] {
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFE,
         };

         ExecutionEngine engine = LoadContract("HubContract");
         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(nowTime);  // args[4] - nowTime
            sb.EmitPush(100);  // args[3] - expiresAfter
            sb.EmitPush(1);  // args[2] - carrySpace
            sb.EmitPush(1);  // args[1] - repRequired
            sb.EmitPush(demand);  // args[0]
            sb.EmitPush(5);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_findMatchableDemand");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(new byte[] {}, result);
      }

      [Fact]
      public void TestFindMatchableDemandFail() {
         ExecutionEngine engine = LoadContract("HubContract");

         var nowTime = 101;
         byte[] expiredExpiry = BitConverter.GetBytes(100).ToArray();
         byte[] futureExpiry = BitConverter.GetBytes(102).ToArray();

         // demand1 - already expired
         var demand1 = expiredExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         // demand2 - expires before supplied `expiresAfter`
         var demand2 = futureExpiry.Concat(new byte[] {
            // expiry (4 byte timestamp) (prepended)
            // itemValue (100000000)
            0x00, 0xE1, 0xF5, 0x05, 0x00,
            // owner script hash
            5, 4, 3, 2, 1, 5, 4, 3, 2, 1,  // line - 10 bytes
            5, 4, 3, 2, 1, 5, 4, 3, 2,
            0xFF,
            // repRequired
            1, 0,
            // itemSize
            1
            // info
         }).Concat(Info).ToArray();

         var demands = demand1.Concat(demand2).ToArray();

         using (ScriptBuilder sb = new ScriptBuilder()) {
            sb.EmitPush(nowTime);  // args[4] - nowTime
            sb.EmitPush(200);  // args[3] - expiresAfter
            sb.EmitPush(1);  // args[2] - carrySpace
            sb.EmitPush(0);  // args[1] - repRequired
            sb.EmitPush(demands);  // args[0]
            sb.EmitPush(5);
            sb.Emit(OpCode.PACK);
            sb.EmitPush("test_demand_findMatchableDemand");  // operation
            ExecuteScript(engine, sb);
         }

         var result = engine.EvaluationStack.Peek().GetByteArray();
         Assert.Equal(new byte[] {}, result);
      }
   }
}
