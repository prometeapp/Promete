using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;

namespace Promete.Windowing.VulkanDesktop;

internal unsafe class VulkanDriver
{
	private readonly Instance _instance;
	private readonly PhysicalDevice _physicalDevice;
	private readonly Device _logicalDevice;
	private readonly Queue _graphicsQueue;

	public VulkanDriver(Silk.NET.Windowing.IWindow window)
	{
		var vk = Vk.GetApi();
		_instance = CreateInstance(vk, window);
		_physicalDevice = PickPhysicalDevice(vk, _instance);
		(_logicalDevice, _graphicsQueue) = CreateLogicalDevice(_physicalDevice, vk);
	}

	/// <summary>
	/// Vulkan インスタンスを作成します。
	/// </summary>
	/// <param name="vk">Vulkan API。</param>
	/// <param name="window">Silk ウィンドウ。</param>
	/// <returns>Vulkan インスタンス。</returns>
	/// <exception cref="InvalidOperationException">Vulkan インスタンスの作成に失敗したときにスローされます。</exception>
	private static Instance CreateInstance(Vk vk, Silk.NET.Windowing.IWindow window)
	{
		var appInfo = new ApplicationInfo
		{
			SType = StructureType.ApplicationInfo,
			PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Promete Application"),
			ApplicationVersion = new Version32(1, 0, 0),
			PEngineName = (byte*)Marshal.StringToHGlobalAnsi("Promete"),
			EngineVersion = new Version32(1, 0, 0),
			ApiVersion = Vk.Version11,
		};

		var createInfo = new InstanceCreateInfo
		{
			SType = StructureType.InstanceCreateInfo,
			PApplicationInfo = &appInfo,
		};

		if (window.VkSurface is { } surface)
		{
			var glfwExtension = surface.GetRequiredExtensions(out var glfwExtensionCount);
			createInfo.EnabledExtensionCount = glfwExtensionCount;
			createInfo.PpEnabledExtensionNames = glfwExtension;
		}

		if (vk.CreateInstance(&createInfo, null, out var instance) != Result.Success)
			throw new InvalidOperationException("Failed to create Vulkan instance.");

		Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
		Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);

		return instance;
	}

	private static PhysicalDevice PickPhysicalDevice(Vk vk, Instance instance)
	{
		var devices = vk.GetPhysicalDevices(instance);

		PhysicalDevice? physicalDevice = null;

		foreach (var device in devices)
		{
			if (GetSuitableFamilyPropertyIndex(device, vk) is null) continue;
			physicalDevice = device;
			break;
		}

		if (physicalDevice is not { } d) throw new Exception("failed to find a suitable GPU!");
		return d;
	}

	private static (Device, Queue) CreateLogicalDevice(PhysicalDevice physicalDevice, Vk vk)
	{
		var index = GetSuitableFamilyPropertyIndex(physicalDevice, vk);

		var queuePriority = 1f;
		var queueCreateInfo = new DeviceQueueCreateInfo
		{
			SType = StructureType.DeviceQueueCreateInfo,
			QueueFamilyIndex = index!.Value,
			QueueCount = 1,
			PQueuePriorities = &queuePriority,
		};

		var deviceFeatures = new PhysicalDeviceFeatures();

		var createInfo = new DeviceCreateInfo
		{
			SType = StructureType.DeviceCreateInfo,
			QueueCreateInfoCount = 1,
			PQueueCreateInfos = &queueCreateInfo,
			PEnabledFeatures = &deviceFeatures,
			EnabledExtensionCount = 0,
			EnabledLayerCount = 0,
		};

		if (vk.CreateDevice(physicalDevice, &createInfo, null, out var logicalDevice) != Result.Success)
			throw new InvalidOperationException("failed to create logical device!");

		vk.GetDeviceQueue(logicalDevice, index.Value, 0, out var queue);

		return (logicalDevice, queue);
	}

	private static uint? GetSuitableFamilyPropertyIndex(PhysicalDevice device, Vk vk)
	{
		uint count = 0;
		vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, null);

		var queueFamilies = stackalloc QueueFamilyProperties[(int)count];
		vk.GetPhysicalDeviceQueueFamilyProperties(device, ref count, queueFamilies);

		for (uint i = 0; i < count; i++)
		{
			if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit)) return count;
		}

		return null;
	}
}
