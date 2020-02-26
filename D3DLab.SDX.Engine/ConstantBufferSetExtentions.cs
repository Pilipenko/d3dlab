using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D3DLab.SDX.Engine {
    public class ConstantBufferSlotsCache : IDisposable {
        readonly HashSet<int> occupied;
        public ConstantBufferSlotsCache() {
            occupied = new HashSet<int>();
        }

        public void Dispose() {
            occupied.Clear();
        }

        public  int GetNext() {
            var max = occupied.Max();
            return max + 1;
        }

        public  bool IsOccupied(int slot) => occupied.Contains(slot);
        public  void SetOccupied(int slot) {
            if (!occupied.Add(slot)) {
                throw new Exception($"Slot {slot} is occupied.");
            }
        }
    }

    public static class ConstantBufferSetExtentions {
        public static void SetConstantBuffer(this CommonShaderStage shader,int slot, SharpDX.Direct3D11.Buffer constantBuffer, ConstantBufferSlotsCache cache) {
            cache.SetOccupied(slot);
            shader.SetConstantBuffer(slot, constantBuffer);            
        }
        public static void SetConstantBufferToNextSlot(this CommonShaderStage shader, SharpDX.Direct3D11.Buffer constantBuffer, ConstantBufferSlotsCache cache) {
            var slot = cache.GetNext();
            shader.SetConstantBuffer(slot, constantBuffer);
        }

    }
}
