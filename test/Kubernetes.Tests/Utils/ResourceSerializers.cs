// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Yarp.Kubernetes.Tests.Utils;

/// <summary>
/// Class ResourceSerializers implements the resource serializers interface.
/// Implements the <see cref="IResourceSerializers" />.
/// </summary>
/// <seealso cref="IResourceSerializers" />
public class ResourceSerializers : IResourceSerializers
{
    private readonly IDeserializer _yamlDeserializer;
    private readonly JsonSerializerSettings _serializationSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSerializers"/> class.
    /// </summary>
    public ResourceSerializers()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNodeTypeResolver(new NonStringScalarTypeResolver())
            .Build();

        _serializationSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter>
            {
                new Iso8601TimeSpanConverter(),
            },
        };
    }

    public T DeserializeYaml<T>(string yaml)
    {
        var resource = _yamlDeserializer.Deserialize<object>(yaml);

        return Convert<T>(resource);
    }

    public T DeserializeJson<T>(string json)
    {
        return SafeJsonConvert.DeserializeObject<T>(json);
    }

    public string SerializeJson(object resource)
    {
        return SafeJsonConvert.SerializeObject(resource, _serializationSettings);
    }

    public TResource Convert<TResource>(object resource)
    {
        var json = SafeJsonConvert.SerializeObject(resource, _serializationSettings);

        return DeserializeJson<TResource>(json);
    }

    private class NonStringScalarTypeResolver : INodeTypeResolver
    {
        bool INodeTypeResolver.Resolve(NodeEvent nodeEvent, ref Type currentType)
        {
            if (currentType == typeof(object) && nodeEvent is Scalar)
            {
                var scalar = nodeEvent as Scalar;
                if (scalar.IsPlainImplicit)
                {
                    // TODO: should use the correct boolean parser (which accepts yes/no) instead of bool.tryparse
                    if (bool.TryParse(scalar.Value, out var _))
                    {
                        currentType = typeof(bool);
                        return true;
                    }

                    if (int.TryParse(scalar.Value, out var _))
                    {
                        currentType = typeof(int);
                        return true;
                    }

                    if (double.TryParse(scalar.Value, out var _))
                    {
                        currentType = typeof(double);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
