using LanguageExt;
using WalletFramework.Core.Functional.Enumerable;
using WalletFramework.Core.Functional.Errors;

namespace WalletFramework.Core.Functional;

public readonly struct Validation<T>
{
    public Validation(Validation<Error, T> value)
    {
        Value = value;
    }
    
    public Validation<Error, T> Value { get; }

    public bool IsSuccess => Value.IsSuccess;
    
    public bool IsFail => Value.IsFail;
    
    public static implicit operator Validation<Error, T>(Validation<T> value) => value.Value;
    
    public static implicit operator Validation<T>(Validation<Error, T> value) => new(value);
    
    public static implicit operator Validation<T>(Error error) => new(Validation<Error, T>.Fail(Seq.create(error)));
    
    public static implicit operator Validation<T>(Seq<Error> errors) => new(Validation<Error, T>.Fail(errors));
    
    public static implicit operator Validation<T>(T value) => new(Validation<Error, T>.Success(value));
}

public delegate Validation<T> Validator<T>(T value);

public delegate Validation<T2> Validator<in T1, T2>(T1 value);

public static class ValidationFun
{
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, Func<T9, Func<T10, TR>>>>>>>>>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, Func<T9, TR>>>>>>>>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, Func<T8, TR>>>>>>>> Apply<T1, T2, T3, T4, T5, T6, T7, T8, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, T6, T7, T8, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, Func<T7, TR>>>>>>> Apply<T1, T2, T3, T4, T5, T6, T7, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, T6, T7, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, Func<T6, TR>>>>>> Apply<T1, T2, T3, T4, T5, T6, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, T6, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, Func<T5, TR>>>>> Apply<T1, T2, T3, T4, T5, TR>(
        this Validation<Func<T1, T2, T3, T4, T5, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, Func<T4, TR>>>> Apply<T1, T2, T3, T4, TR>(
        this Validation<Func<T1, T2, T3, T4, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, Func<T3, TR>>> Apply<T1, T2, T3, TR>(
        this Validation<Func<T1, T2, T3, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<Func<T2, TR>> Apply<T1, T2, TR>(
        this Validation<Func<T1, T2, TR>> valF,
        Validation<T1> valT) =>
        Apply(valF.Select(Prelude.curry), valT);
    
    public static Validation<TR> Apply<T, TR>(
        this Validation<Func<T, TR>> valF,
        Validation<T> valT) =>
        valF.Value.Match(
            f =>
                valT.Value.Match(
                    t => Valid(f(t)),
                    errors => errors
                ),
            errors =>
                valT.Value.Match(
                    _ => errors,
                    errorsT => errors + errorsT
                )
        );
    
    public static T2 Match<TValue, T2>(
        this Validation<TValue> validation,
        Func<TValue, T2> valid,
        Func<Seq<Error>, T2> invalid) =>
        validation.Value.Match(valid, invalid);
    
    public static async Task<T2> Match<TValue, T2>(
        this Task<Validation<TValue>> validation,
        Func<TValue, Task<T2>> valid,
        Func<Seq<Error>, Task<T2>> invalid) =>
        await (await validation).Value.MatchAsync(valid, invalid);
    
    public static Unit Match<TValue>(
        this Validation<TValue> validation,
        Action<TValue> valid,
        Action<Seq<Error>> invalid) =>
        validation.Value.Match(valid, invalid);
    
    public static async Task<Validation<TReturn>> Select<TValue, TReturn>(
        this Task<Validation<TValue>> validation,
        Func<TValue, Task<TReturn>> task) =>
        await (await validation).Value.MatchAsync(
            async value => Valid(await task(value)),
            error => error
        );
    
    public static Validation<TResult> Select<TValue, TResult>(
        this Validation<TValue> validation,
        Func<TValue, TResult> func) =>
        validation.Value.Select(func);
    
    public static async Task<Validation<T3>> SelectMany<T1, T2, T3>(
        this Validation<T1> validation,
        Func<T1, Task<Validation<T2>>> bind,
        Func<T1, T2, T3> project)
    {
        var bindResult =
            await validation.Value.MatchAsync(
                async t => (await bind(t)).Value,
                error => error);
        
        return validation.Value.SelectMany(_ => bindResult, project);
    }
    
    public static async Task<Validation<T3>> SelectMany<T1, T2, T3>(
        this Task<Validation<T1>> validation,
        Func<T1, Task<Validation<T2>>> bind,
        Func<T1, T2, T3> project)
    {
        var validationValue = await validation;
        return await validationValue.SelectMany(bind, project);
    }
    
    public static Validation<T3> SelectMany<T1, T2, T3>(
        this Validation<T1> validation,
        Func<T1, Validation<T2>> bind,
        Func<T1, T2, T3> project) =>
        new(validation.Value.SelectMany(t => bind(t).Value, project));
    
    public static Option<T> ToOption<T>(this Validation<T> validation) => validation.Value.ToOption();

    public static T Fallback<T>(this Validation<T> validation, Func<T> fallbackFunc) =>
        validation.ToOption().Fallback(fallbackFunc);
    
    public static T Fallback<T>(this Validation<T> validation, T fallback) =>
        validation.ToOption().Fallback(fallback);
    
    /// <summary>
    ///     Traverses an enumerable for validation
    /// </summary>
    /// <returns>
    /// <para>A validation of an enumerable of every item.</para>
    /// <para>In case the enumerable is empty, the validation will result in a <see cref="NoItemsSucceededValidationError{T}"/></para>
    /// </returns>
    /// <remarks>The traverse only succeeds when every item of the enumerable is valid. If you want to
    /// ignore all the invalid items and only keep the valid items use <see cref="TraverseAny{T,TR}"/></remarks>
    public static Validation<IEnumerable<T2>> TraverseAll<T1, T2>(
        this IEnumerable<T1> enumerable,
        Func<T1, Validation<T2>> validationFunc)
    {
        var list = enumerable.ToList();
        if (list.IsEmpty())
            return new EnumerableIsEmptyError<T1>();
        
        return list
            .Select(t => validationFunc(t).Value)
            .Traverse(t => t);
    }

    /// <summary>
    ///    Traverses an enumerable for validation
    /// </summary>
    /// <returns>
    /// <para>A validation of an enumerable which contains the items where the validation succeeded</para>
    /// <para>The validation will result in a <see cref="NoItemsSucceededValidationError{T}"/>, in case
    /// no items succeeded the validation</para>
    /// </returns>
    /// <remarks>
    /// The traverse will ignore the invalid items and only keep the valid items. If you want the validation to
    /// only succeed when everything is valid use <see cref="TraverseAll{T,TR}"/></remarks>
    public static Validation<IEnumerable<T2>> TraverseAny<T1, T2>(
        this IEnumerable<T1> enumerable,
        Func<T1, Validation<T2>> validationFunc)
    {
        var items = enumerable.ToList();
        if (items.IsEmpty())
            return new EnumerableIsEmptyError<T2>();
        
        Validation<IEnumerable<T2>> traverse = items
            .Select(t => validationFunc(t).Value)
            .Where(validation => validation.IsSuccess)
            .Traverse(validation => validation);

        return traverse.OnSuccess(traversedItems =>
        {
            Validation<IEnumerable<T2>> result;
            var list = traversedItems.ToList();
            if (list.Any())
                result = list;
            else
                result = new NoItemsSucceededValidationError<T2>();

            return result;
        });
    }

    public static Validation<T2> OnSuccess<T1, T2>(this Validation<T1> validation, Func<T1, Validation<T2>> onSucc) =>
        from t1 in validation
        from t2 in onSucc(t1)
        select t2;

    public static Validation<T2> OnSuccess<T1, T2>(this Validation<T1> validation, Func<T1, T2> onSucc) =>
        from t1 in validation
        let t2 = onSucc(t1)
        select t2;
    
    public static Validation<Unit> OnSuccess<T1>(this Validation<T1> validation, Func<T1, Validation<Unit>> onSucc) =>
        from t1 in validation
        from r in onSucc(t1)
        select r;
    
    public static Task<Validation<Unit>> OnSuccess<T1>(this Validation<T1> validation, Func<T1, Task> onSucc)
    {
        var adapter = new Func<T1, Task<Unit>>(async arg =>
        {
            await onSucc(arg);
            return Unit.Default;
        });

        return validation.OnSuccess(async t1 => await adapter(t1));
    }

    public static Task<Validation<T2>> OnSuccess<T1, T2>(
        this Task<Validation<T1>> validation,
        Func<T1, Task<Validation<T2>>> onSucc) =>
        from t1 in validation
        from t2 in onSucc(t1)
        select t2;

    public static Task<Validation<T2>> OnSuccess<T1, T2>(
        this Validation<T1> validation,
        Func<T1, Task<Validation<T2>>> onSucc) =>
        from t1 in validation
        from t2 in onSucc(t1)
        select t2;
    
    public static Task<Validation<T2>> OnSuccess<T1, T2>(
        this Validation<T1> validation,
        Func<T1, Task<T2>> onSucc) =>
        validation.OnSuccess(async t1 =>
        {
            var taskT2 = onSucc(t1);
            var t2 = await taskT2;
            return Valid(t2);
        });

    public static async Task<Validation<T2>> OnSuccess<T1, T2>(
        this Task<Validation<T1>> validation,
        Func<T1, T2> onSucc)
    {
        var validationT1 = await validation;
        return 
            from t1 in validationT1
            let t2 = onSucc(t1)
            select t2;
    }

    public static Validation<T> Invalid<T>(Seq<Error> errors) => Validation<Error, T>.Fail(errors);

    public static Validation<T> Valid<T>(T value) => value;
    
    public static Validation<T> Flatten<T>(this Validation<Validation<T>> stackedValidation) =>
        from outer in stackedValidation
        from inner in outer
        select inner;

    public static T UnwrapOrThrow<T>(this Validation<T> validation, Exception e) =>
        validation.Match(
            t => t,
            _ => throw e);

    public static T UnwrapOrThrow<T>(this Validation<T> validation) =>
        validation.UnwrapOrThrow(new InvalidOperationException($"Value of Type `{typeof(T)}` is corrupt"));
    
    public static T UnwrapOrThrow<T, TError>(this Validation<TError, T> validation, Exception e) =>
        validation.Match(
            t => t,
            _ => throw e);

    public static T UnwrapOrThrow<T, TError>(this Validation<TError, T> validation) =>
        validation.UnwrapOrThrow(new InvalidOperationException($"Value of Type `{typeof(T)}` is corrupt"));

    public static Validator<T> AggregateValidators<T>(this IEnumerable<Validator<T>> validators) => t =>
    {
        var errors = validators
            .Select(validate => validate(t))
            .SelectMany(validation => validation.Match(
                _ => Option<Seq<Error>>.None,
                errors => errors)
            )
            .SelectMany(seq => seq)
            .ToSeq();

        return errors.Any()
            ? Invalid<T>(errors)
            : t;
    };
    
    /// <summary>
    ///     Iterates through all validators and returns the first validator that succeeded
    /// </summary>
    /// <returns>The first successful validator or <see cref="NoItemsSucceededValidationError{T}"/> when nothing was sucessful</returns>
    /// <remarks>This is early out, which means after the first validator was successful it will not compute the other ones</remarks>
    public static Validator<T1, T2> FirstValid<T1, T2>(this IEnumerable<Validator<T1, T2>> validators) => t =>
    {
        foreach (var validator in validators)
        {
            var x = validator(t);
            if (x.IsSuccess)
            {
                return x;
            }
        }

        return new NoItemsSucceededValidationError<T2>();
    };
}
