using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Physics.Engine.Bepu;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Systems;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace D3DLab.Physics.Engine {

    #region to test

    public class SimpleThreadDispatcher : IThreadDispatcher, IDisposable {
        int threadCount;
        public int ThreadCount => threadCount;
        struct Worker {
            public Thread Thread;
            public AutoResetEvent Signal;
        }

        Worker[] workers;
        AutoResetEvent finished;

        BufferPool[] bufferPools;

        public SimpleThreadDispatcher(int threadCount) {
            this.threadCount = threadCount;
            workers = new Worker[threadCount - 1];
            for (int i = 0; i < workers.Length; ++i) {
                workers[i] = new Worker { Thread = new Thread(WorkerLoop), Signal = new AutoResetEvent(false) };
                workers[i].Thread.IsBackground = true;
                workers[i].Thread.Start(workers[i].Signal);
            }
            finished = new AutoResetEvent(false);
            bufferPools = new BufferPool[threadCount];
            for (int i = 0; i < bufferPools.Length; ++i) {
                bufferPools[i] = new BufferPool();
            }
        }

        void DispatchThread(int workerIndex) {
            Debug.Assert(workerBody != null);
            workerBody(workerIndex);

            if (Interlocked.Increment(ref completedWorkerCounter) == threadCount) {
                finished.Set();
            }
        }

        volatile Action<int> workerBody;
        int workerIndex;
        int completedWorkerCounter;

        void WorkerLoop(object untypedSignal) {
            var signal = (AutoResetEvent)untypedSignal;
            while (true) {
                signal.WaitOne();
                if (disposed)
                    return;
                DispatchThread(Interlocked.Increment(ref workerIndex) - 1);
            }
        }

        void SignalThreads() {
            for (int i = 0; i < workers.Length; ++i) {
                workers[i].Signal.Set();
            }
        }

        public void DispatchWorkers(Action<int> workerBody) {
            Debug.Assert(this.workerBody == null);
            workerIndex = 1; //Just make the inline thread worker 0. While the other threads might start executing first, the user should never rely on the dispatch order.
            completedWorkerCounter = 0;
            this.workerBody = workerBody;
            SignalThreads();
            //Calling thread does work. No reason to spin up another worker and block this one!
            DispatchThread(0);
            finished.WaitOne();
            this.workerBody = null;
        }

        volatile bool disposed;
        public void Dispose() {
            if (!disposed) {
                disposed = true;
                SignalThreads();
                for (int i = 0; i < bufferPools.Length; ++i) {
                    bufferPools[i].Clear();
                }
                foreach (var worker in workers) {
                    worker.Thread.Join();
                    worker.Signal.Dispose();
                }
            }
        }

        public BufferPool GetThreadMemoryPool(int workerIndex) {
            return bufferPools[workerIndex];
        }
    }
    /// <summary>
    /// Bit masks which control whether different members of a group of objects can collide with each other.
    /// </summary>
    public struct SubgroupCollisionFilter {
        /// <summary>
        /// A mask of 16 bits, each set bit representing a collision group that an object belongs to.
        /// </summary>
        public ushort SubgroupMembership;
        /// <summary>
        /// A mask of 16 bits, each set bit representing a collision group that an object can interact with.
        /// </summary>
        public ushort CollidableSubgroups;
        /// <summary>
        /// Id of the owner of the object. Objects belonging to different groups always collide.
        /// </summary>
        public int GroupId;

        /// <summary>
        /// Initializes a collision filter that collides with everything in the group.
        /// </summary>
        /// <param name="groupId">Id of the group that this filter operates within.</param>
        public SubgroupCollisionFilter(int groupId) {
            GroupId = groupId;
            SubgroupMembership = ushort.MaxValue;
            CollidableSubgroups = ushort.MaxValue;
        }

        /// <summary>
        /// Initializes a collision filter that belongs to one specific subgroup and can collide with any other subgroup.
        /// </summary>
        /// <param name="groupId">Id of the group that this filter operates within.</param>
        /// <param name="subgroupId">Id of the subgroup to put this ragdoll</param>
        public SubgroupCollisionFilter(int groupId, int subgroupId) {
            GroupId = groupId;
            Debug.Assert(subgroupId >= 0 && subgroupId < 16, "The subgroup field is a ushort; it can only hold 16 distinct subgroups.");
            SubgroupMembership = (ushort)(1 << subgroupId);
            CollidableSubgroups = ushort.MaxValue;
        }

        /// <summary>
        /// Disables a collision between this filter and the specified subgroup.
        /// </summary>
        /// <param name="subgroupId">Subgroup id to disable collision with.</param>
        public void DisableCollision(int subgroupId) {
            Debug.Assert(subgroupId >= 0 && subgroupId < 16, "The subgroup field is a ushort; it can only hold 16 distinct subgroups.");
            CollidableSubgroups ^= (ushort)(1 << subgroupId);
        }

        /// <summary>
        /// Modifies the interactable subgroups such that filterB does not interact with the subgroups defined by filter a and vice versa.
        /// </summary>
        /// <param name="a">Filter from which to remove collisions with filter b's subgroups.</param>
        /// <param name="b">Filter from which to remove collisions with filter a's subgroups.</param>
        public static void DisableCollision(ref SubgroupCollisionFilter filterA, ref SubgroupCollisionFilter filterB) {
            filterA.CollidableSubgroups &= (ushort)~filterB.SubgroupMembership;
            filterB.CollidableSubgroups &= (ushort)~filterA.SubgroupMembership;
        }

        /// <summary>
        /// Checks if the filters can collide by checking if b's membership can be collided by a's collidable groups.
        /// </summary>
        /// <param name="a">First filter to test.</param>
        /// <param name="b">Second filter to test.</param>
        /// <returns>True if the filters can collide, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllowCollision(in SubgroupCollisionFilter a, in SubgroupCollisionFilter b) {
            return a.GroupId != b.GroupId || (a.CollidableSubgroups & b.SubgroupMembership) > 0;
        }

    }

    struct CarBodyProperties {
        public SubgroupCollisionFilter Filter;
        public float Friction;
    }
    struct CarCallbacks : INarrowPhaseCallbacks {
        public BodyProperty<CarBodyProperties> Properties;
        public void Initialize(Simulation simulation) {
            Properties.Initialize(simulation.Bodies);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b) {
            //It's impossible for two statics to collide, and pairs are sorted such that bodies always come before statics.
            if (b.Mobility != CollidableMobility.Static) {
                return SubgroupCollisionFilter.AllowCollision(Properties[a.Handle].Filter, Properties[b.Handle].Filter);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void CreateMaterial(CollidablePair pair, out PairMaterialProperties pairMaterial) {
            pairMaterial.FrictionCoefficient = Properties[pair.A.Handle].Friction;
            if (pair.B.Mobility != CollidableMobility.Static) {
                //If two bodies collide, just average the friction.
                pairMaterial.FrictionCoefficient = (pairMaterial.FrictionCoefficient + Properties[pair.B.Handle].Friction) * 0.5f;
            }
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = new SpringSettings(30, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, NonconvexContactManifold* manifold, out PairMaterialProperties pairMaterial) {
            CreateMaterial(pair, out pairMaterial);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, ConvexContactManifold* manifold, out PairMaterialProperties pairMaterial) {
            CreateMaterial(pair, out pairMaterial);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ConvexContactManifold* manifold) {
            return true;
        }

        public void Dispose() {
            Properties.Dispose();
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold> {
            throw new NotImplementedException();
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) {
            throw new NotImplementedException();
        }
    }

    //The simulation has a variety of extension points that must be defined. 
    //The demos tend to reuse a few types like the DemoNarrowPhaseCallbacks, but this demo will provide its own (super simple) versions.
    //If you're wondering why the callbacks are interface implementing structs rather than classes or events, it's because 
    //the compiler can specialize the implementation using the compile time type information. That avoids dispatch overhead associated
    //with delegates or virtual dispatch and allows inlining, which is valuable for extremely high frequency logic like contact callbacks.
    //unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks {
    //    /// <summary>
    //    /// Performs any required initialization logic after the Simulation instance has been constructed.
    //    /// </summary>
    //    /// <param name="simulation">Simulation that owns these callbacks.</param>
    //    public void Initialize(Simulation simulation) {
    //        //Often, the callbacks type is created before the simulation instance is fully constructed, so the simulation will call this function when it's ready.
    //        //Any logic which depends on the simulation existing can be put here.
    //    }

    //    /// <summary>
    //    /// Chooses whether to allow contact generation to proceed for two overlapping collidables.
    //    /// </summary>
    //    /// <param name="workerIndex">Index of the worker that identified the overlap.</param>
    //    /// <param name="a">Reference to the first collidable in the pair.</param>
    //    /// <param name="b">Reference to the second collidable in the pair.</param>
    //    /// <returns>True if collision detection should proceed, false otherwise.</returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b) {
    //        //Before creating a narrow phase pair, the broad phase asks this callback whether to bother with a given pair of objects.
    //        //This can be used to implement arbitrary forms of collision filtering. See the RagdollDemo or NewtDemo for examples.
    //        return true;
    //    }

    //    /// <summary>
    //    /// Chooses whether to allow contact generation to proceed for the children of two overlapping collidables in a compound-including pair.
    //    /// </summary>
    //    /// <param name="pair">Parent pair of the two child collidables.</param>
    //    /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
    //    /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
    //    /// <returns>True if collision detection should proceed, false otherwise.</returns>
    //    /// <remarks>This is called for each sub-overlap in a collidable pair involving compound collidables. If neither collidable in a pair is compound, this will not be called.
    //    /// For compound-including pairs, if the earlier call to AllowContactGeneration returns false for owning pair, this will not be called. Note that it is possible
    //    /// for this function to be called twice for the same subpair if the pair has continuous collision detection enabled; 
    //    /// the CCD sweep test that runs before the contact generation test also asks before performing child pair tests.</remarks>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) {
    //        //This is similar to the top level broad phase callback above. It's called by the narrow phase before generating
    //        //subpairs between children in parent shapes. 
    //        //This only gets called in pairs that involve at least one shape type that can contain multiple children, like a Compound.
    //        return true;
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    void ConfigureMaterial(out PairMaterialProperties pairMaterial) {
    //        //The engine does not define any per-body material properties. Instead, all material lookup and blending operations are handled by the callbacks.
    //        //For the purposes of this demo, we'll use the same settings for all pairs.
    //        //(Note that there's no bounciness property! See here for more details: https://github.com/bepu/bepuphysics2/issues/3)
    //        pairMaterial.FrictionCoefficient = 1f;
    //        pairMaterial.MaximumRecoveryVelocity = 2f;
    //        pairMaterial.SpringSettings = new SpringSettings(30, 1);
    //    }

    //    //Note that there is a unique callback for convex versus nonconvex types. There is no fundamental difference here- it's just a matter of convenience
    //    //to avoid working through an interface or casting.
    //    //For the purposes of the demo, contact constraints are always generated.
    //    /// <summary>
    //    /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
    //    /// </summary>
    //    /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
    //    /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
    //    /// <param name="manifold">Set of contacts detected between the collidables.</param>
    //    /// <param name="pairMaterial">Material properties of the manifold.</param>
    //    /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, NonconvexContactManifold* manifold, out PairMaterialProperties pairMaterial) {
    //        ConfigureMaterial(out pairMaterial);
    //        return true;
    //    }

    //    /// <summary>
    //    /// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
    //    /// </summary>
    //    /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
    //    /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
    //    /// <param name="manifold">Set of contacts detected between the collidables.</param>
    //    /// <param name="pairMaterial">Material properties of the manifold.</param>
    //    /// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public unsafe bool ConfigureContactManifold(int workerIndex, CollidablePair pair, ConvexContactManifold* manifold, out PairMaterialProperties pairMaterial) {
    //        ConfigureMaterial(out pairMaterial);
    //        return true;
    //    }

    //    /// <summary>
    //    /// Provides a notification that a manifold has been created between the children of two collidables in a compound-including pair.
    //    /// Offers an opportunity to change the manifold's details. 
    //    /// </summary>
    //    /// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
    //    /// <param name="pair">Pair of collidables that the manifold was detected between.</param>
    //    /// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
    //    /// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
    //    /// <param name="manifold">Set of contacts detected between the collidables.</param>
    //    /// <returns>True if this manifold should be considered for constraint generation, false otherwise.</returns>
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ConvexContactManifold* manifold) {
    //        return true;
    //    }

    //    /// <summary>
    //    /// Releases any resources held by the callbacks. Called by the owning narrow phase when it is being disposed.
    //    /// </summary>
    //    public void Dispose() {
    //    }
    //}
    unsafe struct DemoNarrowPhaseCallbacks : INarrowPhaseCallbacks {
        public SpringSettings ContactSpringiness;

        public void Initialize(Simulation simulation) {
            //Use a default if the springiness value wasn't initialized.
            if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
                ContactSpringiness = new SpringSettings(30, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b) {
            //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
            //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
            //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold> {
            pairMaterial.FrictionCoefficient = 1f;
            pairMaterial.MaximumRecoveryVelocity = 2f;
            pairMaterial.SpringSettings = ContactSpringiness;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) {
            return true;
        }

        public void Dispose() {
        }
    }

    //Note that the engine does not require any particular form of gravity- it, like all the contact callbacks, is managed by a callback.
    public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks {
        public Vector3 Gravity;
        Vector3 gravityDt;

        /// <summary>
        /// Gets how the pose integrator should handle angular velocity integration.
        /// </summary>
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving; //Don't care about fidelity in this demo!

        public PoseIntegratorCallbacks(Vector3 gravity) : this() {
            Gravity = gravity;
        }

        /// <summary>
        /// Called prior to integrating the simulation's active bodies. When used with a substepping timestepper, this could be called multiple times per frame with different time step values.
        /// </summary>
        /// <param name="dt">Current time step duration.</param>
        public void PrepareForIntegration(float dt) {
            //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
            gravityDt = Gravity * dt;
        }

        /// <summary>
        /// Callback called for each active body within the simulation during body integration.
        /// </summary>
        /// <param name="bodyIndex">Index of the body being visited.</param>
        /// <param name="pose">Body's current pose.</param>
        /// <param name="localInertia">Body's current local inertia.</param>
        /// <param name="workerIndex">Index of the worker thread processing this body.</param>
        /// <param name="velocity">Reference to the body's current velocity to integrate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity) {
            //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
            if (localInertia.InverseMass > 0) {
                velocity.Linear = velocity.Linear + gravityDt;
            }
        }

    }
    struct DemoPoseIntegratorCallbacks : IPoseIntegratorCallbacks {
        public Vector3 Gravity;
        public float LinearDamping;
        public float AngularDamping;
        Vector3 gravityDt;
        float linearDampingDt;
        float angularDampingDt;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public DemoPoseIntegratorCallbacks(Vector3 gravity, float linearDamping = .03f, float angularDamping = .03f) : this() {
            Gravity = gravity;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;
        }

        public void PrepareForIntegration(float dt) {
            //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
            gravityDt = Gravity * dt;
            //Since this doesn't use per-body damping, we can precalculate everything.
            linearDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt);
            angularDampingDt = (float)Math.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity) {
            //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
            if (localInertia.InverseMass > 0) {
                velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
                velocity.Angular = velocity.Angular * angularDampingDt;
            }
            //Implementation sidenote: Why aren't kinematics all bundled together separately from dynamics to avoid this per-body condition?
            //Because kinematics can have a velocity- that is what distinguishes them from a static object. The solver must read velocities of all bodies involved in a constraint.
            //Under ideal conditions, those bodies will be near in memory to increase the chances of a cache hit. If kinematics are separately bundled, the the number of cache
            //misses necessarily increases. Slowing down the solver in order to speed up the pose integrator is a really, really bad trade, especially when the benefit is a few ALU ops.

            //Note that you CAN technically modify the pose in IntegrateVelocity by directly accessing it through the Simulation.Bodies.ActiveSet.Poses, it just requires a little care and isn't directly exposed.
            //If the PositionFirstTimestepper is being used, then the pose integrator has already integrated the pose.
            //If the PositionLastTimestepper or SubsteppingTimestepper are in use, the pose has not yet been integrated.
            //If your pose modification depends on the order of integration, you'll want to take this into account.

            //This is also a handy spot to implement things like position dependent gravity or per-body damping.
        }

    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://github.com/bepu/bepuphysics2
    /// </remarks>
    public unsafe class PhysicalSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {

        readonly Simulation simulation;
        readonly BufferPool BufferPool;
        readonly SimpleThreadDispatcher threadDispatcher;
        readonly IPhysicsShapeConstructor constructor;

        public PhysicalSystem() {
            BufferPool = new BufferPool();
            threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
            //  var properties = new BodyProperty<CarBodyProperties>();
            //simulation = BepuPhysics.Simulation.Create(BufferPool,
            //    new CarCallbacks() { Properties = properties },
            //    new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)));

            simulation = Simulation.Create(BufferPool,
                new DemoNarrowPhaseCallbacks(),
                new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)));

            constructor = new BepuPhysicsShapeConstructor(simulation, BufferPool);
        }

        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();

            //Drop a ball on a big static box.
            //simulation.Statics.Add(new StaticDescription(new Vector3(0, 0, 0), new CollidableDescription(simulation.Shapes.Add(new Box(500, 1, 500)), 0.1f)));

            try {
                var comps = new Dictionary<int, GraphicEntity>();
                //var extractor = new BepuBodiesExtractor();

                foreach (var entity in emanager.GetEntities()) {
                    var has = entity.GetComponents<PhysicalComponent>();
                    if (has.Any()) {
                        var phy = has.Single();
                        if (!phy.IsConstructed) {
                            phy.TryConstructBody(entity, constructor);
                        }
                        if (phy.IsConstructed && !phy.IsStatic) {// 
                            comps.Add(phy.ShapeIndex, entity);
                        }
                    }
                }

                simulation.Timestep(1 / 60f);//, threadDispatcher);

                for (int i = 0; i < simulation.Bodies.Sets.Length; ++i) {
                    ref var set = ref simulation.Bodies.Sets[i];
                    if (set.Allocated) { //Islands are stored noncontiguously; skip those which have been deallocated.
                        for (int bodyIndex = 0; bodyIndex < set.Count; ++bodyIndex) {
                            //AddBodyShape(simulation.Shapes, simulation.Bodies, i, bodyIndex);
                            Shapes shapes = simulation.Shapes;
                            Bodies bodies = simulation.Bodies;
                            int setIndex = i;
                            int indexInSet = bodyIndex;

                            ref var activity = ref set.Activity[indexInSet];
                            ref var inertia = ref set.LocalInertias[indexInSet];

                            //AddShape(shapes, set.Collidables[indexInSet].Shape, ref set.Poses[indexInSet], color);
                            // AddShape(Shapes shapes, TypedIndex shapeIndex, ref RigidPose pose, in Vector3 color)
                            ref var shapeIndex = ref set.Collidables[indexInSet].Shape;
                            ref var pose = ref set.Poses[indexInSet];
                         
                            if (shapeIndex.Exists) {
                                shapes[shapeIndex.Type].GetShapeData(shapeIndex.Index, out var shapeData, out _);
                                //AddShape(shapeData, shapeIndex.Type, shapes, ref pose, color);
                                //AddShape(void* shapeData, int shapeType, Shapes shapes, ref RigidPose pose, in Vector3 color)
                                var shapeType = shapeIndex.Type;

                                switch (shapeType) {
                                    case Mesh.Id:
                                        //ref var mesh = ref Unsafe.AsRef<Mesh>(shapeData);
                                        break;
                                    case Box.Id:
                                        var pp = pose.Position;
                                        var r = pose.Orientation;
                                        
                                        var q = System.Numerics.Quaternion.Conjugate(new System.Numerics.Quaternion(r.X, r.Y, r.Z, r.W));
                                        var m = Matrix4x4.CreateFromQuaternion(new System.Numerics.Quaternion(r.X, r.Y, r.Z, r.W));

                                        //m = Matrix4x4.Transpose(m);

                                        var entity = comps[shapeIndex.Index];
                                        var com = entity.GetComponent<IBepuDynamicPhysicalComponent>();

                                        if (!m.IsIdentity) {
                                            var toZero = Matrix4x4.CreateTranslation(Vector3.Zero - com.AABBox.GetCenter());
                                            m = toZero * m * toZero.Inverted();
                                        }
                                        var newm = m * Matrix4x4.CreateTranslation(pp - com.AABBox.GetCenter());
                                        if (!newm.IsIdentity) {
                                            entity.UpdateComponent(TransformComponent.Create(newm));
                                        }
                                        break;
                                }
                            }

                        }
                    }
                }
                return;
                for (int i = 0; i < simulation.Bodies.Sets.Length; ++i) {
                    ref var set = ref simulation.Bodies.Sets[i];
                    if (set.Allocated) {//Islands are stored noncontiguously; skip those which have been deallocated.
                        for (int bodyIndex = 0; bodyIndex < set.Count; ++bodyIndex) {
                            var shapeIndex = set.Collidables[bodyIndex].Shape;
                            if (!shapeIndex.Exists) {
                                return;
                            }
                            ref var pose = ref set.Poses[bodyIndex];
                            var pp = pose.Position;
                            var r = pose.Orientation;
                            var m = Matrix4x4.CreateFromQuaternion(new System.Numerics.Quaternion(r.X, r.Y, r.Z, r.W));

                            var entity = comps[shapeIndex.Index];
                            var com = entity.GetComponent<IBepuDynamicPhysicalComponent>();

                            if (!m.IsIdentity) {
                                var toZero = Matrix4x4.CreateTranslation(Vector3.Zero - com.AABBox.GetCenter());
                                m = toZero * m * toZero.Inverted();
                            }
                            var newm = m * Matrix4x4.CreateTranslation(pp - com.AABBox.GetCenter());
                            if (!newm.IsIdentity) {
                                //entity
                                //    .GetComponent<TransformComponent>()
                                //    .MatrixWorld = newm;

                                entity.UpdateComponent(TransformComponent.Create(newm));
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                throw ex;
                //Console.WriteLine(ex.Message);
            }

        }

        public void Dispose() {
            simulation.Dispose();
            BufferPool.Clear();
            threadDispatcher.Dispose();
        }


        class BepuPhysicsShapeConstructor : IPhysicsShapeConstructor {
            readonly Simulation simulation;
            readonly BufferPool bufferPool;

            public BepuPhysicsShapeConstructor(Simulation simulation, BufferPool bufferPool) {
                this.simulation = simulation;
                this.bufferPool = bufferPool;

                //bufferPool.Take<Vector3>(3, out var vertices1);
                //bufferPool.Return(ref vertices1);

            }

            public bool TryConstructShape(GraphicEntity entity, DynamicAABBPhysicalComponent physicalComponent) {
                var hasGeo = entity.GetComponents<IGeometryComponent>();
                if (!hasGeo.Any()) {
                    return false;
                }

                //return false;

                var geo = hasGeo.First();

                var box = geo.Box;
                var size = box.Size();

                if (size.IsZero()) {
                    return false;
                }

                //var fbox = new Box(20f, 10f, 20f);
                //fbox.ComputeInertia(1, out var sphereInertia);

                //var t = simulation.Shapes.Add(fbox);
                //physicalComponent.ShapeIndex = t.Index;

                //simulation.Bodies.Add(BodyDescription.CreateDynamic(
                //    new Vector3(64, 100, 32),
                //    sphereInertia,
                //    new CollidableDescription(t, 0.1f),
                //    new BodyActivityDescription(0.01f)));

                //fbox.ComputeBounds(BepuUtilities.Quaternion.Identity, out var min, out var max);
                //physicalComponent.AABBox = new Std.Engine.Core.Utilities.BoundingBox(min, max);

                var fbox = new Box(size.X, size.Y, size.Z);
                fbox.ComputeInertia(1, out var sphereInertia);

                var t = simulation.Shapes.Add(fbox);
                physicalComponent.ShapeIndex = t.Index;

                simulation.Bodies.Add(BodyDescription.CreateDynamic(
                    box.GetCenter(),
                    sphereInertia,
                    new CollidableDescription(t, 0.1f),
                    new BodyActivityDescription(0.01f)));

                physicalComponent.AABBox = box;

                physicalComponent.IsConstructed = true;

                return true;
            }

            public bool TryConstructShape(GraphicEntity entity, StaticAABBPhysicalComponent physicalComponent) {
                var hasGeo = entity.GetComponents<IGeometryComponent>();
                if (!hasGeo.Any()) {
                    return false;
                }
                var geo = hasGeo.First();

                if (physicalComponent.IsConstructed && geo.IsModified) {
                    //update!!
                }

                var box = geo.Box;
                var size = box.Size();

                if (size.IsZero()) {
                    return false;
                }
                var tr = entity.GetComponent<TransformComponent>();
                if (!tr.MatrixWorld.IsIdentity) {
                    box = box.Transform(tr.MatrixWorld);
                }

                var index = simulation.Shapes.Add(new Box(size.X, size.Y, size.Z));
                physicalComponent.ShapeIndex = index.Index;
                simulation.Statics.Add(
                    new StaticDescription(box.GetCenter(), new CollidableDescription(index, 0.1f))
                    );



                physicalComponent.IsConstructed = true;
                physicalComponent.IsStatic = true;

                return true;
            }

            public bool TryConstructShape(GraphicEntity entity, StaticMeshPhysicalComponent physicalComponent) {
                try {
                    var hasGeo = entity.GetComponents<IGeometryComponent>();
                    if (!hasGeo.Any()) {
                        //throw new PhysicsException("Can't construct shape, no GeometryComponent");
                        return false;
                    }
                    var geo = hasGeo.First();

                    if (physicalComponent.IsConstructed && geo.IsModified) {
                        //update!!
                    }

                    var tr = entity.GetComponent<TransformComponent>();
                    var matrix = Matrix4x4.Identity;// tr.MatrixWorld;
                    //var geo2 = geo;

                  //  entity.UpdateComponent(TransformComponent.Identity());

                   // var box = geo.Box.Transform(matrix);
                    // + Vector3.UnitY *-2;
                    var vv = new Vector3[geo.Positions.Length];
                    for (int i = 0; i < geo.Positions.Length; ++i) {
                        vv[i] = geo.Positions[i];//.TransformedCoordinate(matrix);
                    }

                    var box = D3DLab.Std.Engine.Core.Utilities.BoundingBox.CreateFromVertices(vv);
                    var center = box.GetCenter();

                    bufferPool.Take<Vector3>(geo.Positions.Length, out var vertices);
                    for (int i = 0; i < geo.Positions.Length; ++i) {
                        vertices[i] = vv[i];
                    }

                    var triangleCount = (int)geo.Indices.Length / 3;
                    bufferPool.Take<Triangle>(triangleCount, out var triangles);

                    var itr = 0;
                    for (var i = 0; i < geo.Indices.Length; i += 3) {
                        triangles[itr] = new Triangle(
                            vertices[geo.Indices[i + 0]],
                            vertices[geo.Indices[i + 1]],
                            vertices[geo.Indices[i + 2]]);
                        itr++;
                    }
                    bufferPool.Return(ref vertices);
                    var planeMesh = new Mesh(triangles, new Vector3(1, 1, 1), bufferPool);


                    //const int planeWidth = 256;//48
                    //const int planeHeight = 256;
                    //CreateDeformedPlane(planeWidth, planeHeight,
                    //    (int x, int y) => {
                    //        Vector2 offsetFromCenter = new Vector2(x - planeWidth / 2, y - planeHeight / 2);
                    //        return new Vector3(offsetFromCenter.X, (float)Math.Cos(x / 4f) * (float)Math.Sin(y / 4f) - 0.01f * offsetFromCenter.LengthSquared(), offsetFromCenter.Y);
                    //    }, new Vector3(1, 1, 1), bufferPool, out var planeMesh, out var geo2);
                    ////new Vector3(64, 4, 32) - geo2.Box.GetCenter()
                    //var move = TransformComponent.Create(Matrix4x4.CreateTranslation(
                    //    new Vector3(127.5f, 0, 127.5f) - geo2.Box.GetCenter()));

                    //entity.RemoveComponents<SimpleGeometryComponent>();
                    //entity.UpdateComponent(geo2);
                    //entity.UpdateComponent(move);

                    //var box = geo2.Box.Transform(move.MatrixWorld);
                    //var center = box.GetCenter();

                    center.Y = box.Maximum.Y;

                    var tindex1  = simulation.Shapes.Add(planeMesh);
                    physicalComponent.ShapeIndex = tindex1.Index;
                    simulation.Statics.Add(new StaticDescription(center,//new Vector3(64, 10, 32),
                        BepuUtilities.Quaternion.Identity,//.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / 2),
                        new CollidableDescription(tindex1, 0.1f)));
                    
                    physicalComponent.IsConstructed = true;
                    physicalComponent.IsStatic = true;

                    simulation.Shapes[tindex1.Type].GetShapeData(tindex1.Index, out var shapeData, out _);
                    ref var mesh = ref Unsafe.AsRef<Mesh>(shapeData);


                    return true;
                    /*
                    var mesh = new Mesh(triangles, new Vector3(1, 1, 1), bufferPool);

                    var tindex = simulation.Shapes.Add(mesh);
                    physicalComponent.ShapeIndex = tindex.Index;
                    var pose = new RigidPose {
                        Position = new Vector3(0,30,0),
                       // Orientation = BepuUtilities.Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)Math.PI / 2f),
                        Orientation = BepuUtilities.Quaternion.Identity 
                    };

                    var groundDescription = new StaticDescription {
                        Collidable = new CollidableDescription {
                            Shape = tindex,
                            SpeculativeMargin = 0.1f,
                        },
                        Pose = pose
                    };

                    simulation.Statics.Add(groundDescription);

                    physicalComponent.IsConstructed = true;
                    physicalComponent.IsStatic = true;

                    return true;*/
                } catch (Exception ex) {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
                return false;
            }

            public bool TryConstructShape(GraphicEntity entity, DynamicMeshPhysicalComponent physicalComponent) {
                try {
                    var hasGeo = entity.GetComponents<IGeometryComponent>();
                    if (!hasGeo.Any()) {
                        //throw new PhysicsException("Can't construct shape, no GeometryComponent");
                        return false;
                    }
                    var geo = hasGeo.First();

                    if (physicalComponent.IsConstructed && geo.IsModified) {
                        //update!!
                    }

                    var tr = entity.GetComponent<TransformComponent>();
                    var matrix = tr.MatrixWorld;
                    var box = geo.Box.Transform(matrix);
                    var center = box.GetCenter();

                    physicalComponent.AABBox = box;

                    bufferPool.Take<Vector3>(geo.Positions.Length, out var vertices);
                    for (int i = 0; i < geo.Positions.Length; ++i) {
                        vertices[i] = geo.Positions[i].TransformedCoordinate(matrix);
                    }

                    var triangleCount = (int)geo.Indices.Length / 3;
                    bufferPool.Take<Triangle>(triangleCount, out var triangles);

                    var itr = 0;
                    for (var i = 0; i < geo.Indices.Length; i += 3) {
                        triangles[itr] = new Triangle(
                            vertices[geo.Indices[i + 0]],
                            vertices[geo.Indices[i + 1]],
                            vertices[geo.Indices[i + 2]]);
                        itr++;
                    }
                    bufferPool.Return(ref vertices);

                    var mesh = new Mesh(triangles, new Vector3(1, 1, 1), bufferPool);
                    mesh.ComputeClosedInertia(10, out var meshInertia, out var meshPosition);

                    var gridShape = new Sphere(box.Size().Y);
                    gridShape.ComputeInertia(1, out meshInertia);


                    //var tindex = simulation.Shapes.Add(mesh);

                    var tindex = simulation.Shapes.Add(gridShape);

                    physicalComponent.ShapeIndex = tindex.Index;

                    var pose = new RigidPose(meshPosition);
                    var description = BodyDescription.CreateDynamic(
                        pose,
                        meshInertia,
                        new CollidableDescription(tindex, 0.1f),
                        new BodyActivityDescription(0.01f)
                        );

                    simulation.Bodies.Add(description);

                    physicalComponent.IsConstructed = true;

                    return true;
                } catch (Exception ex) {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                }
                return false;
            }

            static void CreateDeformedPlane(int width, int height, Func<int, int, Vector3> deformer, Vector3 scaling, BufferPool pool, out Mesh mesh, out HittableGeometryComponent geo) {
                var pos = new List<Vector3>();
                var inds = new List<int>();

                pool.Take<Vector3>(width * height, out var vertices);
                for (int i = 0; i < width; ++i) {
                    for (int j = 0; j < height; ++j) {
                        var v = deformer(i, j);
                        pos.Add(v);
                        vertices[width * j + i] = v;
                    }
                }

                var quadWidth = width - 1;
                var quadHeight = height - 1;
                var triangleCount = quadWidth * quadHeight * 2;
                pool.Take<Triangle>(triangleCount, out var triangles);

                for (int i = 0; i < quadWidth; ++i) {
                    for (int j = 0; j < quadHeight; ++j) {
                        var triangleIndex = (j * quadWidth + i) * 2;
                        ref var triangle0 = ref triangles[triangleIndex];
                        ref var v00 = ref vertices[width * j + i];
                        ref var v01 = ref vertices[width * j + i + 1];
                        ref var v10 = ref vertices[width * (j + 1) + i];
                        ref var v11 = ref vertices[width * (j + 1) + i + 1];
                        triangle0.A = v00;
                        triangle0.B = v01;
                        triangle0.C = v10;
                        ref var triangle1 = ref triangles[triangleIndex + 1];
                        triangle1.A = v01;
                        triangle1.B = v11;
                        triangle1.C = v10;

                        inds.AddRange(new[] {
                            width * j + i, width * j + i + 1, width * (j + 1) + i
                        });
                        inds.AddRange(new[] {
                            width * j + i + 1, width * (j + 1) + i + 1, width * (j + 1) + i
                        });
                    }
                }
                
                geo = new HittableGeometryComponent {
                    Positions = pos.ToImmutableArray(),
                    Indices = inds.ToImmutableArray(),
                    Normals = pos.CalculateNormals(inds).ToImmutableArray(),
                    Color = new Vector4(1, 0, 0, 1)
                };

                pool.Return(ref vertices);
                mesh = new Mesh(triangles, scaling, pool);

             
            }
        }
    }

    interface IPhysicsShapeConstructor {
        bool TryConstructShape(GraphicEntity entity, StaticAABBPhysicalComponent physicalComponent);
        bool TryConstructShape(GraphicEntity entity, DynamicAABBPhysicalComponent physicalComponent);
        bool TryConstructShape(GraphicEntity entity, StaticMeshPhysicalComponent physicalComponent);
        bool TryConstructShape(GraphicEntity entity, DynamicMeshPhysicalComponent physicalComponent);

    }
}
